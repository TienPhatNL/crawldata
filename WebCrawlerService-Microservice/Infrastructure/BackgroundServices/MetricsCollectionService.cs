using WebCrawlerService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebCrawlerService.Domain.Enums;
using WebCrawlerService.Infrastructure.Common;
using WebCrawlerService.Infrastructure.Contexts;

namespace WebCrawlerService.Infrastructure.BackgroundServices;

public class MetricsCollectionService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MetricsCollectionService> _logger;
    private readonly TimeSpan _collectionInterval = TimeSpan.FromMinutes(5);

    public MetricsCollectionService(IServiceProvider serviceProvider, ILogger<MetricsCollectionService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for database migration to complete before starting
        _logger.LogInformation("MetricsCollectionService waiting for database migration to complete...");

        while (!StartupCoordinator.IsMigrationCompleted && !stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }

        if (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("MetricsCollectionService cancelled before migration completed");
            return;
        }

        _logger.LogInformation("✅ Database ready - Metrics collection service starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CollectSystemMetrics(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during metrics collection");
            }

            try
            {
                await Task.Delay(_collectionInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Metrics collection service stopped");
    }

    private async Task CollectSystemMetrics(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrawlerDbContext>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

        // Collect job statistics
        var jobStats = await context.CrawlJobs
            .GroupBy(j => j.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(stoppingToken);

        // Collect agent statistics
        var agentStats = await context.CrawlerAgents
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(stoppingToken);

        // Collect recent performance metrics
        var last24Hours = DateTime.UtcNow.AddHours(-24);
        var recentJobsCompleted = await context.CrawlJobs
            .Where(j => j.CompletedAt >= last24Hours)
            .CountAsync(stoppingToken);

        var averageProcessingTime = await context.CrawlJobs
            .Where(j => j.CompletedAt >= last24Hours && j.StartedAt.HasValue && j.CompletedAt.HasValue)
            .Select(j => EF.Functions.DateDiffSecond(j.StartedAt, j.CompletedAt))
            .DefaultIfEmpty(0)
            .AverageAsync(stoppingToken);

        var successRate = await context.CrawlJobs
            .Where(j => j.CompletedAt >= last24Hours)
            .GroupBy(j => j.Status == JobStatus.Completed)
            .Select(g => new { IsSuccess = g.Key, Count = g.Count() })
            .ToListAsync(stoppingToken);

        // Calculate success percentage
        var totalRecentJobs = successRate.Sum(s => s.Count);
        var successfulJobs = successRate.FirstOrDefault(s => s.IsSuccess)?.Count ?? 0;
        var successPercentage = totalRecentJobs > 0 ? (double)successfulJobs / totalRecentJobs * 100 : 0;

        // Cache metrics for quick retrieval
        var metrics = new SystemMetrics
        {
            JobStatistics = jobStats.ToDictionary(s => s.Status.ToString(), s => s.Count),
            AgentStatistics = agentStats.ToDictionary(s => s.Status.ToString(), s => s.Count),
            JobsCompletedLast24Hours = recentJobsCompleted,
            AverageProcessingTimeSeconds = 0,
            SuccessRateLast24Hours = successPercentage,
            CollectedAt = DateTime.UtcNow
        };

        await cacheService.SetAsync("system:metrics", metrics, TimeSpan.FromMinutes(10), stoppingToken);

        _logger.LogDebug("Collected system metrics - Jobs completed: {CompletedJobs}, Success rate: {SuccessRate:F2}%, Avg processing time: {AvgTime:F2}s", 
            recentJobsCompleted, successPercentage, averageProcessingTime);

        // Log system health warnings
        var activeAgents = agentStats.FirstOrDefault(s => s.Status == AgentStatus.Active)?.Count ?? 0;
        var unhealthyAgents = agentStats.FirstOrDefault(s => s.Status == AgentStatus.Unhealthy)?.Count ?? 0;
        var pendingJobs = jobStats.FirstOrDefault(s => s.Status == JobStatus.Pending)?.Count ?? 0;

        if (activeAgents == 0 && pendingJobs > 0)
        {
            _logger.LogWarning("No active agents available but {PendingJobs} jobs are pending", pendingJobs);
        }

        if (unhealthyAgents > activeAgents / 2)
        {
            _logger.LogWarning("High number of unhealthy agents: {UnhealthyCount} unhealthy vs {ActiveCount} active", 
                unhealthyAgents, activeAgents);
        }

        if (successPercentage < 80 && totalRecentJobs > 10)
        {
            _logger.LogWarning("Low success rate in last 24 hours: {SuccessRate:F2}% ({SuccessfulJobs}/{TotalJobs})", 
                successPercentage, successfulJobs, totalRecentJobs);
        }
    }
}

public class SystemMetrics
{
    public Dictionary<string, int> JobStatistics { get; set; } = new();
    public Dictionary<string, int> AgentStatistics { get; set; } = new();
    public int JobsCompletedLast24Hours { get; set; }
    public double AverageProcessingTimeSeconds { get; set; }
    public double SuccessRateLast24Hours { get; set; }
    public DateTime CollectedAt { get; set; }
}