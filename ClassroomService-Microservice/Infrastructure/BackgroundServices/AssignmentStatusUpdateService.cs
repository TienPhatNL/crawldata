using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that automatically updates assignment statuses based on dates
/// Runs every hour to check and update:
/// - Scheduled -> Active (when StartDate is reached or passed)
/// - Active -> Extended (when DueDate passed but ExtendedDueDate not reached)
/// - Active/Extended -> Overdue (when all due dates passed)
/// Note: Draft assignments are NOT auto-activated - they must be manually scheduled first
/// </summary>
public class AssignmentStatusUpdateService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AssignmentStatusUpdateService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5); // Run every 5 minutes

    public AssignmentStatusUpdateService(
        IServiceProvider serviceProvider,
        ILogger<AssignmentStatusUpdateService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Assignment Status Update Service started");

        // Wait a bit before first run (30 seconds after startup)
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await UpdateAssignmentStatusesAsync(stoppingToken);
                
                // Wait for next check interval
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Assignment Status Update Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Assignment Status Update Service");
                
                // Wait 5 minutes before retrying on error
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Assignment Status Update Service stopped");
    }

    private async Task UpdateAssignmentStatusesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // Convert UTC to UTC+7 (SE Asia Standard Time) for comparison
        var utcNow = DateTime.UtcNow;
        var seAsiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var now = TimeZoneInfo.ConvertTimeFromUtc(utcNow, seAsiaTimeZone);
        
        _logger.LogInformation("Starting automatic assignment status update check at {UtcTime} UTC / {LocalTime} UTC+7", utcNow, now);

        try
        {
            // Get all assignments that are not Closed (permanent state)
            // Note: Graded status is obsolete - using Report.Status = Graded instead
            var assignments = await unitOfWork.Assignments.GetManyAsync(
                a => a.Status != AssignmentStatus.Closed,
                cancellationToken);

            if (!assignments.Any())
            {
                _logger.LogInformation("No assignments found to update");
                return;
            }

            var updatedCount = 0;
            var statusChanges = new Dictionary<string, int>
            {
                ["Scheduled->Active"] = 0,
                ["Active->Extended"] = 0,
                ["Active->Overdue"] = 0,
                ["Extended->Overdue"] = 0,
                ["Scheduled->Overdue"] = 0
            };

            foreach (var assignment in assignments)
            {
                var oldStatus = assignment.Status;
                var newStatus = DetermineStatus(assignment, now);

                if (oldStatus != newStatus)
                {
                    assignment.Status = newStatus;
                    assignment.UpdatedAt = utcNow; // Save as UTC in database
                    
                    // Get course to get lecturer ID
                    var course = await unitOfWork.Courses.GetAsync(c => c.Id == assignment.CourseId, cancellationToken);
                    
                    // Add domain event for automatic status change
                    assignment.AddDomainEvent(new Domain.Events.AssignmentStatusChangedEvent(
                        assignment.Id,
                        assignment.CourseId,
                        assignment.Title,
                        oldStatus,
                        newStatus,
                        course?.LecturerId ?? Guid.Empty,
                        isAutomatic: true));
                    
                    updatedCount++;

                    var changeKey = $"{oldStatus}->{newStatus}";
                    if (statusChanges.ContainsKey(changeKey))
                    {
                        statusChanges[changeKey]++;
                    }

                    _logger.LogInformation(
                        "Updated assignment {AssignmentId} ({Title}) status from {OldStatus} to {NewStatus}",
                        assignment.Id, assignment.Title, oldStatus, newStatus);
                }
            }

            if (updatedCount > 0)
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation(
                    "Assignment status update completed. Updated {Count} assignment(s). Changes: {Changes}",
                    updatedCount,
                    string.Join(", ", statusChanges.Where(kvp => kvp.Value > 0).Select(kvp => $"{kvp.Key}: {kvp.Value}")));
            }
            else
            {
                _logger.LogInformation("No assignment status changes required");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating assignment statuses");
            throw;
        }
    }

    /// <summary>
    /// Determines the correct status for an assignment based on current time
    /// Status transition rules:
    /// - Draft: Stays Draft (manual scheduling required)
    /// - Scheduled: Becomes Active when StartDate arrives
    /// - Active: Becomes Extended when DueDate passes (if ExtendedDueDate set)
    /// - Active/Extended/Scheduled: Becomes Overdue when all due dates pass
    /// - Closed: Permanent state, never change
    /// Note: Graded status is obsolete - use Report.Status = ReportStatus.Graded instead
    /// </summary>
    private static AssignmentStatus DetermineStatus(Domain.Entities.Assignment assignment, DateTime now)
    {
        // Permanent state - never change automatically
        if (assignment.Status == AssignmentStatus.Closed)
        {
            return assignment.Status;
        }

        // Draft assignments are NOT auto-activated - must be manually scheduled
        if (assignment.Status == AssignmentStatus.Draft)
        {
            return AssignmentStatus.Draft;
        }

        // Determine effective due date
        var effectiveDueDate = assignment.ExtendedDueDate ?? assignment.DueDate;

        // Check if overdue (past all due dates)
        if (effectiveDueDate < now)
        {
            return AssignmentStatus.Overdue;
        }

        // Check if extended (DueDate passed but ExtendedDueDate not yet)
        if (assignment.ExtendedDueDate.HasValue && assignment.DueDate < now && assignment.ExtendedDueDate.Value >= now)
        {
            return AssignmentStatus.Extended;
        }

        // Scheduled -> Active transition when StartDate arrives
        if (assignment.Status == AssignmentStatus.Scheduled)
        {
            if (!assignment.StartDate.HasValue || assignment.StartDate.Value <= now)
            {
                return AssignmentStatus.Active;
            }
            return AssignmentStatus.Scheduled; // Keep scheduled until StartDate
        }

        // For Active assignments, stay active if not past due date
        if (assignment.Status == AssignmentStatus.Active)
        {
            return AssignmentStatus.Active;
        }

        // Default: keep current status if no rules apply
        return assignment.Status;
    }
}
