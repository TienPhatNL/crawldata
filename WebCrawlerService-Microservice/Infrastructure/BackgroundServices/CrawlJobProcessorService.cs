using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebCrawlerService.Domain.Interfaces;
using WebCrawlerService.Domain.Enums;
using WebCrawlerService.Infrastructure.Common;
using WebCrawlerService.Infrastructure.Contexts;
using WebCrawlerService.Infrastructure.Services;

namespace WebCrawlerService.Infrastructure.BackgroundServices;

public class CrawlJobProcessorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CrawlJobProcessorService> _logger;
    private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(10);
    private readonly int _maxConcurrentJobs = Environment.ProcessorCount;

    public CrawlJobProcessorService(IServiceProvider serviceProvider, ILogger<CrawlJobProcessorService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for database migration to complete before starting
        _logger.LogInformation("CrawlJobProcessorService waiting for database migration to complete...");

        while (!StartupCoordinator.IsMigrationCompleted && !stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }

        if (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("CrawlJobProcessorService cancelled before migration completed");
            return;
        }

        _logger.LogInformation("✅ Database ready - Crawl job processor service starting with max concurrent jobs: {MaxJobs}", _maxConcurrentJobs);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingJobs(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing crawl jobs");
            }

            try
            {
                await Task.Delay(_processingInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Crawl job processor service stopped");
    }

    private async Task ProcessPendingJobs(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrawlerDbContext>();
        
        // Get pending jobs ordered by priority and creation time
        var pendingJobs = await context.CrawlJobs
            .Where(j => j.Status == JobStatus.Pending)
            .OrderBy(j => j.Priority)
            .ThenBy(j => j.CreatedAt)
            .Take(_maxConcurrentJobs)
            .ToListAsync(stoppingToken);

        if (!pendingJobs.Any())
            return;

        _logger.LogDebug("Found {JobCount} pending jobs to process", pendingJobs.Count);

        var processingTasks = pendingJobs.Select(async job =>
        {
            using var jobScope = _serviceProvider.CreateScope();

            try
            {
                await ProcessSingleJobAsync(jobScope, job, stoppingToken);
                _logger.LogDebug("Successfully processed crawl job {JobId}", job.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process crawl job {JobId}", job.Id);
                await MarkJobAsFailedAsync(jobScope, job.Id, ex.Message, stoppingToken);
            }
        });

        await Task.WhenAll(processingTasks);
    }

    private async Task ProcessSingleJobAsync(
        IServiceScope scope,
        Domain.Entities.CrawlJob job,
        CancellationToken cancellationToken)
    {
        var context = scope.ServiceProvider.GetRequiredService<CrawlerDbContext>();
        var agentFactory = scope.ServiceProvider.GetRequiredService<CrawlerAgentFactory>();

        // Update job status to Running
        job.Status = JobStatus.Running;
        job.StartedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Processing job {JobId} with {UrlCount} URLs", job.Id, job.Urls.Length);

        try
        {
            // Get appropriate crawler agent
            var agent = await agentFactory.GetAgentAsync(job, cancellationToken);

            if (agent == null)
            {
                _logger.LogWarning(
                    "No suitable agent found for job {JobId} with crawler type {CrawlerType}",
                    job.Id, job.CrawlerType);
                throw new InvalidOperationException(
                    $"No suitable crawler agent available for type {job.CrawlerType}");
            }

            _logger.LogInformation("Using agent {AgentName} for job {JobId}",
                agent.AgentName, job.Id);

            // Execute crawl
            var results = await agent.ExecuteAsync(job, cancellationToken);

            // Save results to database
            foreach (var result in results)
            {
                context.CrawlResults.Add(result);
            }

            // Update job statistics
            job.UrlsProcessed = results.Count;
            job.UrlsSuccessful = results.Count(r => r.IsSuccess);
            job.UrlsFailed = results.Count(r => !r.IsSuccess);
            job.TotalContentSize = results.Sum(r => r.ContentSize);
            job.Status = JobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Job {JobId} completed: {SuccessCount}/{TotalCount} successful",
                job.Id, job.UrlsSuccessful, job.UrlsProcessed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing job {JobId}", job.Id);
            throw;
        }
    }

    private async Task MarkJobAsFailedAsync(
        IServiceScope scope,
        Guid jobId,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            var context = scope.ServiceProvider.GetRequiredService<CrawlerDbContext>();
            var job = await context.CrawlJobs.FindAsync(new object[] { jobId }, cancellationToken);

            if (job != null)
            {
                job.Status = JobStatus.Failed;
                job.FailedAt = DateTime.UtcNow;
                job.ErrorMessage = errorMessage.Length > 1000
                    ? errorMessage[..1000]
                    : errorMessage;

                await context.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark job {JobId} as failed", jobId);
        }
    }
}