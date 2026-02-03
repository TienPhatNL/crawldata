using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that automatically closes assignments when completion criteria are met:
/// Closes assignment when BOTH conditions are satisfied:
/// 1. Grace period (3 days) has passed after effective due date (extended or original)
/// 2. AND either:
///    a) All student/group reports are graded (ReportStatus.Graded), OR
///    b) Assignment is overdue for 7+ days (abandoned assignment fallback)
/// 
/// Runs every 30 minutes to check Active, Extended, and Overdue assignments.
/// For individual assignments: Checks if all enrolled students have graded reports
/// For group assignments: Checks if all assigned groups have graded reports
/// 
/// This approach provides grace period for:
/// - Late submissions
/// - Grade corrections and disputes
/// - Administrative adjustments
/// While still ensuring abandoned assignments eventually close automatically.
/// </summary>
public class AssignmentCompletionService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AssignmentCompletionService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(8);
    private readonly TimeSpan _gracePeriodsAfterDueDate = TimeSpan.FromDays(3); // Grace period for corrections
    private readonly TimeSpan _overdueGracePeriod = TimeSpan.FromDays(7); // Fallback for abandoned assignments

    public AssignmentCompletionService(
        IServiceProvider serviceProvider,
        ILogger<AssignmentCompletionService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Assignment Completion Service started");

        // Wait before first run (1 minute after startup)
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndCloseCompletedAssignmentsAsync(stoppingToken);
                
                // Wait for next check interval
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Assignment Completion Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Assignment Completion Service");
                
                // Wait 5 minutes before retrying on error
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Assignment Completion Service stopped");
    }

    private async Task CheckAndCloseCompletedAssignmentsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var utcNow = DateTime.UtcNow;
        var seAsiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var now = TimeZoneInfo.ConvertTimeFromUtc(utcNow, seAsiaTimeZone);
        
        _logger.LogInformation("Starting assignment completion check at {UtcTime} UTC / {LocalTime} UTC+7", utcNow, now);

        try
        {
            // Get all assignments that are Active, Extended, or Overdue (candidates for auto-close)
            var assignments = await unitOfWork.Assignments.GetManyAsync(
                a => a.Status == AssignmentStatus.Active || 
                     a.Status == AssignmentStatus.Extended || 
                     a.Status == AssignmentStatus.Overdue,
                cancellationToken);

            if (!assignments.Any())
            {
                _logger.LogInformation("No active/extended/overdue assignments found to check");
                return;
            }

            var closedCount = 0;
            var allGradedCount = 0;
            var overdueExpiredCount = 0;

            foreach (var assignment in assignments)
            {
                var shouldClose = false;
                var reason = string.Empty;

                // Get effective due date (extended due date takes priority)
                var effectiveDueDate = assignment.ExtendedDueDate ?? assignment.DueDate;
                
                // Check if grace period (3 days) has passed after effective due date
                var gracePeriodPassed = effectiveDueDate.Add(_gracePeriodsAfterDueDate) < now;
                
                // Check if assignment is overdue for 7+ days (abandoned assignment fallback)
                var isOverdueExpired = effectiveDueDate.Add(_overdueGracePeriod) < now;

                if (!gracePeriodPassed)
                {
                    // Grace period hasn't passed yet - don't close even if all graded
                    // This gives instructors time for corrections and students time for late submissions
                    continue;
                }

                // Grace period has passed - now check closure conditions
                if (isOverdueExpired)
                {
                    // Condition: 7+ days overdue (abandoned assignment)
                    shouldClose = true;
                    reason = $"Grace period passed ({_gracePeriodsAfterDueDate.Days} days) and overdue for {_overdueGracePeriod.Days}+ days";
                    overdueExpiredCount++;
                }
                else
                {
                    // Check if all reports are graded
                    var allGraded = await AreAllReportsGradedAsync(unitOfWork, assignment, cancellationToken);
                    if (allGraded)
                    {
                        // Condition: Grace period passed AND all reports graded
                        shouldClose = true;
                        reason = $"Grace period passed ({_gracePeriodsAfterDueDate.Days} days after due date) and all reports graded";
                        allGradedCount++;
                    }
                }

                if (shouldClose)
                {
                    var oldStatus = assignment.Status;
                    assignment.Status = AssignmentStatus.Closed;
                    assignment.UpdatedAt = utcNow;
                    
                    // Get enrolled student IDs for event
                    var course = await unitOfWork.Courses.GetAsync(c => c.Id == assignment.CourseId, cancellationToken);
                    var enrollments = await unitOfWork.CourseEnrollments.GetManyAsync(
                        e => e.CourseId == assignment.CourseId && e.Status == EnrollmentStatus.Active,
                        cancellationToken);
                    var enrolledStudentIds = enrollments.Select(e => e.StudentId).ToList();
                    
                    // Add domain event for automatic assignment closure
                    assignment.AddDomainEvent(new Domain.Events.AssignmentClosedEvent(
                        assignment.Id,
                        assignment.CourseId,
                        assignment.Title,
                        utcNow,
                        enrolledStudentIds));
                    
                    closedCount++;

                    _logger.LogInformation(
                        "Auto-closing assignment {AssignmentId} ({Title}). Status: {OldStatus} -> Closed. Reason: {Reason}",
                        assignment.Id, assignment.Title, oldStatus, reason);
                }
            }

            if (closedCount > 0)
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation(
                    "Assignment completion check completed. Closed {Count} assignment(s). " +
                    "All graded: {AllGraded}, Overdue expired: {OverdueExpired}",
                    closedCount, allGradedCount, overdueExpiredCount);
            }
            else
            {
                _logger.LogInformation("No assignments ready to be closed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking assignment completion");
            throw;
        }
    }

    /// <summary>
    /// Checks if all reports for an assignment are graded
    /// For individual assignments: Checks all enrolled students
    /// For group assignments: Checks all assigned groups
    /// </summary>
    private async Task<bool> AreAllReportsGradedAsync(
        IUnitOfWork unitOfWork, 
        Domain.Entities.Assignment assignment,
        CancellationToken cancellationToken)
    {
        // Get all reports for this assignment
        var reports = await unitOfWork.Reports.GetManyAsync(
            r => r.AssignmentId == assignment.Id,
            cancellationToken);

        if (assignment.IsGroupAssignment)
        {
            // For group assignments: Check if all assigned groups have graded reports
            var assignedGroups = await unitOfWork.Groups.GetManyAsync(
                g => g.AssignmentId == assignment.Id,
                cancellationToken);

            if (!assignedGroups.Any())
            {
                _logger.LogWarning(
                    "Group assignment {AssignmentId} has no assigned groups",
                    assignment.Id);
                return false; // No groups assigned, can't be complete
            }

            var assignedGroupIds = assignedGroups.Select(g => g.Id).ToHashSet();
            var gradedGroupIds = reports
                .Where(r => r.IsGroupSubmission && r.Status == ReportStatus.Graded && r.GroupId.HasValue)
                .Select(r => r.GroupId!.Value)
                .ToHashSet();

            var allGroupsGraded = assignedGroupIds.All(gid => gradedGroupIds.Contains(gid));

            _logger.LogDebug(
                "Assignment {AssignmentId}: {GradedCount}/{TotalCount} groups graded",
                assignment.Id, gradedGroupIds.Count, assignedGroupIds.Count);

            return allGroupsGraded;
        }
        else
        {
            // For individual assignments: Check if all enrolled students have graded reports
            var course = await unitOfWork.Courses.GetAsync(
                c => c.Id == assignment.CourseId,
                cancellationToken);

            if (course == null)
            {
                _logger.LogWarning(
                    "Assignment {AssignmentId} has no associated course",
                    assignment.Id);
                return false;
            }

            var enrollments = await unitOfWork.CourseEnrollments.GetManyAsync(
                e => e.CourseId == course.Id && e.Status == EnrollmentStatus.Active,
                cancellationToken);

            if (!enrollments.Any())
            {
                _logger.LogWarning(
                    "Assignment {AssignmentId} course has no active enrollments",
                    assignment.Id);
                return false; // No students enrolled, can't be complete
            }

            var enrolledStudentIds = enrollments.Select(e => e.StudentId).ToHashSet();
            var gradedStudentIds = reports
                .Where(r => !r.IsGroupSubmission && r.Status == ReportStatus.Graded)
                .Select(r => r.SubmittedBy)
                .ToHashSet();

            var allStudentsGraded = enrolledStudentIds.All(sid => gradedStudentIds.Contains(sid));

            _logger.LogDebug(
                "Assignment {AssignmentId}: {GradedCount}/{TotalCount} students graded",
                assignment.Id, gradedStudentIds.Count, enrolledStudentIds.Count);

            return allStudentsGraded;
        }
    }
}
