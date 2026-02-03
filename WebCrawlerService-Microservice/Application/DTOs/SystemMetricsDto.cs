namespace WebCrawlerService.Application.DTOs;

/// <summary>
/// System-wide metrics for monitoring dashboard
/// </summary>
public class SystemMetricsDto
{
    public int ActiveJobs { get; set; }
    public int QueuedJobs { get; set; }
    public int TotalAgents { get; set; }
    public int HealthyAgents { get; set; }
    public int UnhealthyAgents { get; set; }
    public double AvgJobCompletionTimeMs { get; set; }
    public double SuccessRate { get; set; }
    public DateTime CollectedAt { get; set; }
}

/// <summary>
/// Overall system health status
/// </summary>
public class SystemHealthDto
{
    public int ActiveJobs { get; set; }
    public int QueuedJobs { get; set; }
    public int HealthyAgents { get; set; }
    public int UnhealthyAgents { get; set; }
    public DateTime Timestamp { get; set; }
    public string HealthStatus => GetHealthStatus();

    private string GetHealthStatus()
    {
        if (UnhealthyAgents > HealthyAgents)
            return "Critical";
        if (QueuedJobs > ActiveJobs * 2)
            return "Warning";
        return "Healthy";
    }
}

/// <summary>
/// Individual agent status
/// </summary>
public class AgentStatusDto
{
    public Guid AgentId { get; set; }
    public string Name { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string CrawlerType { get; set; } = null!;
    public DateTime? LastHeartbeat { get; set; }
    public int CurrentJobCount { get; set; }
    public int TotalJobsCompleted { get; set; }
}

/// <summary>
/// Job statistics for a date range
/// </summary>
public class JobStatisticsDto
{
    public int TotalJobs { get; set; }
    public int CompletedJobs { get; set; }
    public int FailedJobs { get; set; }
    public double AverageCompletionTimeMs { get; set; }
    public int TotalUrlsCrawled { get; set; }
    public long TotalDataExtracted { get; set; }
}

/// <summary>
/// User's quota consumption status
/// </summary>
public class UserQuotaStatusDto
{
    public Guid UserId { get; set; }
    public string SubscriptionTier { get; set; } = null!;
    public int QuotaLimit { get; set; }
    public int QuotaUsed { get; set; }
    public int QuotaRemaining { get; set; }
    public double UsagePercentage { get; set; }
    public DateTime? ResetDate { get; set; }
}
