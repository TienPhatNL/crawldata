using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebCrawlerService.Domain.Enums;
using WebCrawlerService.Infrastructure.Common;
using WebCrawlerService.Infrastructure.Contexts;

namespace WebCrawlerService.Infrastructure.BackgroundServices;

public class JobSchedulingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JobSchedulingService> _logger;
    private readonly TimeSpan _schedulingInterval = TimeSpan.FromSeconds(30);

    public JobSchedulingService(IServiceProvider serviceProvider, ILogger<JobSchedulingService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for database migration to complete before starting
        _logger.LogInformation("JobSchedulingService waiting for database migration to complete...");

        while (!StartupCoordinator.IsMigrationCompleted && !stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }

        if (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("JobSchedulingService cancelled before migration completed");
            return;
        }

        _logger.LogInformation("✅ Database ready - Job scheduling service starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await AssignJobsToAgents(stoppingToken);
                await HandleFailedJobs(stoppingToken);
                await HandleTimedOutJobs(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during job scheduling");
            }

            try
            {
                await Task.Delay(_schedulingInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Job scheduling service stopped");
    }

    private async Task AssignJobsToAgents(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrawlerDbContext>();

        // Get available agents
        var availableAgents = await context.CrawlerAgents
            .Where(a => a.Status == AgentStatus.Active)
            .Where(a => a.CurrentJobCount < a.MaxConcurrentJobs)
            .OrderBy(a => a.CurrentJobCount) // Prefer agents with lower load
            .ThenBy(a => a.LastAssignedAt ?? DateTime.MinValue)
            .ToListAsync(stoppingToken);

        if (!availableAgents.Any())
            return;

        // Get pending jobs ordered by priority
        var pendingJobs = await context.CrawlJobs
            .Where(j => j.Status == JobStatus.Pending && j.AssignedAgentId == null)
            .OrderBy(j => j.Priority)
            .ThenBy(j => j.CreatedAt)
            .Take(availableAgents.Sum(a => a.MaxConcurrentJobs - a.CurrentJobCount))
            .ToListAsync(stoppingToken);

        var assignedCount = 0;
        foreach (var job in pendingJobs)
        {
            var suitableAgent = availableAgents
                .Where(a => a.Type == job.CrawlerType || a.Type == CrawlerType.Universal)
                .Where(a => a.CurrentJobCount < a.MaxConcurrentJobs)
                .OrderBy(a => a.CurrentJobCount)
                .FirstOrDefault();

            if (suitableAgent != null)
            {
                job.AssignedAgentId = suitableAgent.Id;
                job.Status = JobStatus.Assigned;
                job.UpdatedAt = DateTime.UtcNow;

                suitableAgent.CurrentJobCount++;
                suitableAgent.LastAssignedAt = DateTime.UtcNow;
                suitableAgent.UpdatedAt = DateTime.UtcNow;

                assignedCount++;

                _logger.LogDebug("Assigned job {JobId} to agent {AgentId} ({AgentName})", 
                    job.Id, suitableAgent.Id, suitableAgent.Name);
            }
        }

        if (assignedCount > 0)
        {
            await context.SaveChangesAsync(stoppingToken);
            _logger.LogInformation("Assigned {JobCount} jobs to agents", assignedCount);
        }
    }

    private async Task HandleFailedJobs(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrawlerDbContext>();

        var failedRetryableJobs = await context.CrawlJobs
            .Where(j => j.Status == JobStatus.Failed)
            .Where(j => j.RetryCount < j.MaxRetries)
            .Where(j => j.NextRetryAt <= DateTime.UtcNow)
            .ToListAsync(stoppingToken);

        foreach (var job in failedRetryableJobs)
        {
            job.Status = JobStatus.Pending;
            job.AssignedAgentId = null;
            job.ErrorMessage = null;
            job.UpdatedAt = DateTime.UtcNow;
            job.NextRetryAt = null;

            _logger.LogInformation("Retrying failed job {JobId} (attempt {RetryCount}/{MaxRetries})", 
                job.Id, job.RetryCount + 1, job.MaxRetries);
        }

        if (failedRetryableJobs.Any())
        {
            await context.SaveChangesAsync(stoppingToken);
        }
    }

    private async Task HandleTimedOutJobs(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrawlerDbContext>();

        var timeoutThreshold = DateTime.UtcNow.AddMinutes(-30); // Jobs running for more than 30 minutes

        var timedOutJobs = await context.CrawlJobs
            .Where(j => j.Status == JobStatus.InProgress)
            .Where(j => j.StartedAt < timeoutThreshold)
            .ToListAsync(stoppingToken);

        foreach (var job in timedOutJobs)
        {
            _logger.LogWarning("Job {JobId} has timed out - resetting for retry", job.Id);
            
            job.Status = JobStatus.Failed;
            job.ErrorMessage = "Job timed out";
            job.FailedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;
            job.RetryCount++;
            
            // Calculate exponential backoff for retry
            var delayMinutes = Math.Pow(2, job.RetryCount);
            job.NextRetryAt = DateTime.UtcNow.AddMinutes(delayMinutes);

            // Release the agent
            if (job.AssignedAgent != null)
            {
                job.AssignedAgent.CurrentJobCount = Math.Max(0, job.AssignedAgent.CurrentJobCount - 1);
                job.AssignedAgent.UpdatedAt = DateTime.UtcNow;
            }
            
            job.AssignedAgentId = null;
        }

        if (timedOutJobs.Any())
        {
            await context.SaveChangesAsync(stoppingToken);
            _logger.LogInformation("Reset {JobCount} timed out jobs", timedOutJobs.Count);
        }
    }
}