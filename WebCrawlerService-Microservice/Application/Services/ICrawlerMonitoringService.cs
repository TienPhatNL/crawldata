using WebCrawlerService.Application.DTOs;
using WebCrawlerService.Domain.Entities;
using WebCrawlerService.Domain.Enums;

namespace WebCrawlerService.Application.Services;

public interface ICrawlerMonitoringService
{
    Task<CrawlerHealthStatus> GetCrawlerHealthAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<CrawlerAgentStatus>> GetAgentStatusesAsync(CancellationToken cancellationToken = default);
    Task<JobStatistics> GetJobStatisticsAsync(DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    Task<UserQuotaStatus> GetUserQuotaStatusAsync(Guid userId, CancellationToken cancellationToken = default);
    Task LogJobProgressAsync(Guid jobId, string message, CancellationToken cancellationToken = default);
    Task NotifyJobStatusChangeAsync(Guid jobId, JobStatus newStatus, CancellationToken cancellationToken = default);

    // New methods for real-time tracking
    Task<JobStatsDto?> GetJobStatsAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<SystemMetricsDto> GetSystemMetricsAsync(CancellationToken cancellationToken = default);
}

public class CrawlerHealthStatus
{
    public bool IsHealthy { get; set; }
    public int ActiveJobs { get; set; }
    public int AvailableAgents { get; set; }
    public int QueueLength { get; set; }
    public DateTime LastChecked { get; set; }
}

public class CrawlerAgentStatus
{
    public Guid AgentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public AgentStatus Status { get; set; }
    public int ActiveJobs { get; set; }
    public DateTime LastActive { get; set; }
}

public class JobStatistics
{
    public int TotalJobs { get; set; }
    public int CompletedJobs { get; set; }
    public int FailedJobs { get; set; }
    public int PendingJobs { get; set; }
    public double AverageProcessingTime { get; set; }
    public long TotalContentCrawled { get; set; }
}

public class UserQuotaStatus
{
    public Guid UserId { get; set; }
    public SubscriptionTier Tier { get; set; }
    public int JobsUsed { get; set; }
    public int JobsLimit { get; set; }
    public long DataUsed { get; set; }
    public long DataLimit { get; set; }
    public DateTime ResetDate { get; set; }
}