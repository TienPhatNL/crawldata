namespace WebCrawlerService.Application.DTOs;

/// <summary>
/// Real-time statistics for a crawl job
/// </summary>
public class JobStatsDto
{
    public Guid JobId { get; set; }
    public string Status { get; set; } = null!;
    public int TotalUrls { get; set; }
    public int CompletedUrls { get; set; }
    public int FailedUrls { get; set; }
    public double ProgressPercentage { get; set; }
    public int AvgResponseTimeMs { get; set; }
    public long TotalContentSize { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EstimatedCompletion { get; set; }
    public TimeSpan? ElapsedTime { get; set; }
    public string? CurrentUrl { get; set; }
    public double SuccessRate { get; set; }
}
