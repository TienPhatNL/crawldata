using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using WebCrawlerService.Application.DTOs;
using WebCrawlerService.Application.Hubs;
using WebCrawlerService.Domain.Entities;
using WebCrawlerService.Domain.Enums;
using WebCrawlerService.Domain.Interfaces;

namespace WebCrawlerService.Application.Services;

public class CrawlerMonitoringService : ICrawlerMonitoringService
{
    private readonly IRepository<CrawlJob> _jobRepo;
    private readonly IRepository<CrawlerAgent> _agentRepo;
    private readonly IRepository<CrawlResult> _resultRepo;
    private readonly IUserQuotaService _quotaService;
    private readonly IDistributedCache _cache;
    private readonly IHubContext<CrawlHub> _hubContext;
    private readonly ILogger<CrawlerMonitoringService> _logger;

    public CrawlerMonitoringService(
        IRepository<CrawlJob> jobRepo,
        IRepository<CrawlerAgent> agentRepo,
        IRepository<CrawlResult> resultRepo,
        IUserQuotaService quotaService,
        IDistributedCache cache,
        IHubContext<CrawlHub> hubContext,
        ILogger<CrawlerMonitoringService> logger)
    {
        _jobRepo = jobRepo;
        _agentRepo = agentRepo;
        _resultRepo = resultRepo;
        _quotaService = quotaService;
        _cache = cache;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<CrawlerHealthStatus> GetCrawlerHealthAsync(CancellationToken cancellationToken = default)
    {
        var jobs = await _jobRepo.GetAsync(null, null, null, null, cancellationToken);
        var agents = await _agentRepo.GetAsync(null, null, null, null, cancellationToken);

        var activeJobs = jobs.Count(j => j.Status == JobStatus.Running || j.Status == JobStatus.InProgress);
        var availableAgents = agents.Count(a => a.Status == AgentStatus.Active);
        var queueLength = jobs.Count(j => j.Status == JobStatus.Queued || j.Status == JobStatus.Pending);

        return new CrawlerHealthStatus
        {
            IsHealthy = availableAgents > 0 && queueLength < (availableAgents * 5),
            ActiveJobs = activeJobs,
            AvailableAgents = availableAgents,
            QueueLength = queueLength,
            LastChecked = DateTime.UtcNow
        };
    }

    public async Task<IEnumerable<CrawlerAgentStatus>> GetAgentStatusesAsync(CancellationToken cancellationToken = default)
    {
        var agents = await _agentRepo.GetAsync(null, null, null, null, cancellationToken);

        return agents.Select(a => new CrawlerAgentStatus
        {
            AgentId = a.Id,
            Name = a.Name,
            Status = a.Status,
            ActiveJobs = a.CurrentJobCount,
            LastActive = a.LastHeartbeat ?? a.UpdatedAt ?? a.CreatedAt
        }).ToList();
    }

    public async Task<JobStatistics> GetJobStatisticsAsync(
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var jobs = await _jobRepo.GetAsync(null, null, null, null, cancellationToken);

        var startDate = from ?? DateTime.UtcNow.AddDays(-30);
        var endDate = to ?? DateTime.UtcNow;

        var filtered = jobs.Where(j =>
            j.CreatedAt >= startDate && j.CreatedAt <= endDate).ToList();

        var completedJobs = filtered.Where(j =>
            j.Status == JobStatus.Completed && j.CompletedAt.HasValue && j.StartedAt.HasValue).ToList();

        var averageProcessingTime = completedJobs.Any()
            ? completedJobs.Average(j => (j.CompletedAt!.Value - j.StartedAt!.Value).TotalMilliseconds)
            : 0;

        return new JobStatistics
        {
            TotalJobs = filtered.Count,
            CompletedJobs = filtered.Count(j => j.Status == JobStatus.Completed),
            FailedJobs = filtered.Count(j => j.Status == JobStatus.Failed),
            PendingJobs = filtered.Count(j => j.Status == JobStatus.Pending || j.Status == JobStatus.Queued),
            AverageProcessingTime = averageProcessingTime,
            TotalContentCrawled = filtered.Sum(j => j.TotalContentSize)
        };
    }

    public async Task<UserQuotaStatus> GetUserQuotaStatusAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var quotaInfo = await _quotaService.GetQuotaInfoAsync(userId, cancellationToken);
        var jobs = await _jobRepo.GetAsync(null, null, null, null, cancellationToken);
        var userJobs = jobs.Where(j => j.UserId == userId).ToList();

        var jobsUsed = 0;
        var jobsLimit = 0;
        var tier = SubscriptionTier.Free;
        var resetDate = DateTime.UtcNow.AddMonths(1);

        if (quotaInfo != null && quotaInfo.TotalQuota > 0)
        {
            jobsLimit = quotaInfo.TotalQuota;
            jobsUsed = Math.Max(0, quotaInfo.TotalQuota - quotaInfo.RemainingQuota);
            resetDate = quotaInfo.ResetDate ?? resetDate;

            if (!string.IsNullOrWhiteSpace(quotaInfo.PlanType) &&
                Enum.TryParse<SubscriptionTier>(quotaInfo.PlanType, true, out var parsedTier))
            {
                tier = parsedTier;
            }
        }
        else
        {
            jobsUsed = userJobs.Count;
            jobsLimit = 100;
        }

        var dataLimit = tier == SubscriptionTier.Free ? 104857600L : 10737418240L; // 100MB or 10GB

        return new UserQuotaStatus
        {
            UserId = userId,
            Tier = tier,
            JobsUsed = jobsUsed,
            JobsLimit = jobsLimit,
            DataUsed = userJobs.Sum(j => j.TotalContentSize),
            DataLimit = dataLimit,
            ResetDate = resetDate
        };
    }

    public async Task LogJobProgressAsync(Guid jobId, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Job {JobId}: {Message}", jobId, message);

        // Broadcast log message to clients
        await _hubContext.Clients
            .Group($"job_{jobId}")
            .SendAsync("OnJobLog", new
            {
                jobId,
                message,
                level = "Information",
                timestamp = DateTime.UtcNow
            }, cancellationToken);
    }

    public async Task NotifyJobStatusChangeAsync(Guid jobId, JobStatus newStatus, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients
            .Group($"job_{jobId}")
            .SendAsync("OnJobStatusChanged", new
            {
                jobId,
                status = newStatus.ToString(),
                timestamp = DateTime.UtcNow
            }, cancellationToken);
    }

    public async Task<JobStatsDto?> GetJobStatsAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await _jobRepo.GetByIdAsync(jobId, cancellationToken);
        if (job == null)
            return null;

        var results = await _resultRepo.GetAsync(null, null, null, null, cancellationToken);
        var jobResults = results.Where(r => r.CrawlJobId == jobId).ToList();

        var avgResponseTime = jobResults.Any()
            ? (int)jobResults.Average(r => r.ResponseTimeMs)
            : 0;

        var elapsedTime = job.StartedAt.HasValue
            ? DateTime.UtcNow - job.StartedAt.Value
            : TimeSpan.Zero;

        var progressPercentage = job.Urls.Length > 0
            ? (double)job.UrlsProcessed / job.Urls.Length * 100
            : 0;

        var successRate = job.UrlsProcessed > 0
            ? (double)job.UrlsSuccessful / job.UrlsProcessed * 100
            : 0;

        // Estimate completion time
        DateTime? estimatedCompletion = null;
        if (job.Status == JobStatus.Running && job.UrlsProcessed > 0 && avgResponseTime > 0)
        {
            var remainingUrls = job.Urls.Length - job.UrlsProcessed;
            var estimatedRemainingMs = remainingUrls * avgResponseTime;
            estimatedCompletion = DateTime.UtcNow.AddMilliseconds(estimatedRemainingMs);
        }

        return new JobStatsDto
        {
            JobId = job.Id,
            Status = job.Status.ToString(),
            TotalUrls = job.Urls.Length,
            CompletedUrls = job.UrlsSuccessful,
            FailedUrls = job.UrlsFailed,
            ProgressPercentage = progressPercentage,
            AvgResponseTimeMs = avgResponseTime,
            TotalContentSize = job.TotalContentSize,
            StartedAt = job.StartedAt,
            EstimatedCompletion = estimatedCompletion,
            ElapsedTime = elapsedTime,
            CurrentUrl = null, // Will be updated by progress events
            SuccessRate = successRate
        };
    }

    public async Task<SystemMetricsDto> GetSystemMetricsAsync(CancellationToken cancellationToken = default)
    {
        // Try to get cached metrics first
        var cacheKey = "system_metrics";
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);

        if (cached != null)
        {
            try
            {
                return JsonSerializer.Deserialize<SystemMetricsDto>(cached)!;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize cached system metrics");
            }
        }

        // Compute fresh metrics
        var health = await GetCrawlerHealthAsync(cancellationToken);
        var stats = await GetJobStatisticsAsync(
            DateTime.UtcNow.AddHours(-24),
            DateTime.UtcNow,
            cancellationToken);

        var agents = await _agentRepo.GetAsync(null, null, null, null, cancellationToken);

        var metrics = new SystemMetricsDto
        {
            ActiveJobs = health.ActiveJobs,
            QueuedJobs = health.QueueLength,
            TotalAgents = agents.Count(),
            HealthyAgents = agents.Count(a => a.Status == AgentStatus.Active),
            UnhealthyAgents = agents.Count(a => a.Status == AgentStatus.Unhealthy),
            AvgJobCompletionTimeMs = stats.AverageProcessingTime,
            SuccessRate = stats.TotalJobs > 0
                ? (double)stats.CompletedJobs / stats.TotalJobs * 100
                : 0,
            CollectedAt = DateTime.UtcNow
        };

        // Cache for 30 seconds
        try
        {
            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(metrics),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache system metrics");
        }

        return metrics;
    }
}
