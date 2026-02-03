using ClassroomService.Domain.Enums;
using ClassroomService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClassroomService.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that automatically activates terms when their start date arrives
/// and deactivates terms when their end date passes.
/// Also triggers course deactivation for all courses in deactivated terms.
/// Runs every hour to check term status.
/// </summary>
public class TermAutoActivationService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<TermAutoActivationService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(6); // Check every 6 minutes

    public TermAutoActivationService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<TermAutoActivationService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Term Auto-Activation Service started - checking every {Minutes} minute(s)", 
            _checkInterval.TotalMinutes);

        // Wait for initial startup to complete
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("⏰ Running term status check...");
                await ProcessTermActivationAsync(stoppingToken);
                _logger.LogInformation("✅ Term status check completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing term activation/deactivation");
            }

            // Wait for next check interval
            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("🛑 Term Auto-Activation Service stopped");
    }

    private async Task ProcessTermActivationAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ClassroomDbContext>();
        
        // Convert UTC to UTC+7 (SE Asia Standard Time) for comparison
        var utcNow = DateTime.UtcNow;
        var seAsiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var now = TimeZoneInfo.ConvertTimeFromUtc(utcNow, seAsiaTimeZone);

        _logger.LogInformation("📅 Checking term activation/deactivation status at {UtcTime} UTC / {LocalTime} UTC+7", utcNow, now);

        // Get all terms
        var terms = await context.Terms
            .Include(t => t.Courses)
            .ToListAsync(cancellationToken);

        var activatedCount = 0;
        var deactivatedCount = 0;
        var coursesDeactivatedCount = 0;

        foreach (var term in terms)
        {
            var shouldBeActive = term.StartDate.Date <= now.Date && term.EndDate.Date >= now.Date;

            // Activate terms that should be active but aren't
            if (shouldBeActive && !term.IsActive)
            {
                term.IsActive = true;
                term.UpdatedAt = DateTime.UtcNow;
                activatedCount++;
                
                _logger.LogInformation(
                    "Activated term '{TermName}' (Period: {StartDate} - {EndDate})",
                    term.Name,
                    term.StartDate.ToString("yyyy-MM-dd"),
                    term.EndDate.ToString("yyyy-MM-dd"));
            }
            // Deactivate terms that should not be active but are
            else if (!shouldBeActive && term.IsActive)
            {
                term.IsActive = false;
                term.UpdatedAt = DateTime.UtcNow;
                deactivatedCount++;
                
                _logger.LogInformation(
                    "Deactivated term '{TermName}' (Period: {StartDate} - {EndDate})",
                    term.Name,
                    term.StartDate.ToString("yyyy-MM-dd"),
                    term.EndDate.ToString("yyyy-MM-dd"));

                // Deactivate all courses in this term
                var coursesInTerm = term.Courses.Where(c => c.Status == CourseStatus.Active).ToList();
                foreach (var course in coursesInTerm)
                {
                    course.Status = CourseStatus.Inactive;
                    course.UpdatedAt = DateTime.UtcNow;
                    coursesDeactivatedCount++;
                    
                    _logger.LogInformation(
                        "Deactivated course '{CourseName}' (ID: {CourseId}) due to term deactivation",
                        course.Name,
                        course.Id);
                }
            }
        }

        if (activatedCount > 0 || deactivatedCount > 0 || coursesDeactivatedCount > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation(
                "Term activation/deactivation completed: {ActivatedCount} activated, {DeactivatedCount} deactivated, {CoursesDeactivated} courses deactivated",
                activatedCount,
                deactivatedCount,
                coursesDeactivatedCount);
        }
        else
        {
            _logger.LogInformation("ℹ️ No term status changes required");
        }
    }
}
