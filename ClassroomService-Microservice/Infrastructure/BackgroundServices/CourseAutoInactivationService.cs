using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that automatically inactivates courses from past years
/// Runs daily at midnight
/// </summary>
public class CourseAutoInactivationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CourseAutoInactivationService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(7); // Run every 7 minutes

    public CourseAutoInactivationService(
        IServiceProvider serviceProvider,
        ILogger<CourseAutoInactivationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Course Auto-Inactivation Service started - checking every {Minutes} minute(s)", 
            _checkInterval.TotalMinutes);

        // Wait before first run
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("⏰ Running course inactivation check...");
                await InactivatePastYearCoursesAsync(stoppingToken);
                _logger.LogInformation("✅ Course inactivation check completed");
                
                // Wait until next run
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Course Auto-Inactivation Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Course Auto-Inactivation Service");
                
                // Wait a bit before retrying on error
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("🛑 Course Auto-Inactivation Service stopped");
    }

    private async Task InactivatePastYearCoursesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // Convert UTC to UTC+7 (SE Asia Standard Time) for comparison
        var utcNow = DateTime.UtcNow;
        var seAsiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var now = TimeZoneInfo.ConvertTimeFromUtc(utcNow, seAsiaTimeZone);

        _logger.LogInformation("📚 Checking courses for inactivation at {UtcTime} UTC / {LocalTime} UTC+7", utcNow, now);

        // Find all active courses where the term has ended
        var expiredCourses = await unitOfWork.Courses.GetManyAsync(
            c => c.Status == CourseStatus.Active,
            cancellationToken,
            c => c.Term);

        // Filter courses where term end date has passed
        var coursesToInactivate = expiredCourses.Where(c =>
            c.Term.EndDate.Date < now.Date
        ).ToList();

        if (!coursesToInactivate.Any())
        {
            _logger.LogInformation("ℹ️ No courses with past term end dates found to inactivate");
            return;
        }

        _logger.LogInformation("Found {Count} active courses with past term end dates", coursesToInactivate.Count);

        // Inactivate all courses with past term end dates
        foreach (var course in coursesToInactivate)
        {
            course.Status = CourseStatus.Inactive;
            
            _logger.LogInformation(
                "Auto-inactivated course: {CourseId} - {CourseName} (Term: {TermName}, End Date: {EndDate})",
                course.Id,
                course.Name,
                course.Term.Name,
                course.Term.EndDate.ToString("yyyy-MM-dd"));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("📊 Successfully inactivated {Count} course(s) with past term end dates", coursesToInactivate.Count);
    }
}
