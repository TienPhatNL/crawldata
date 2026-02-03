using WebCrawlerService.Domain.Common;

namespace WebCrawlerService.Domain.Events;

/// <summary>
/// Published periodically to track job progress
/// </summary>
public class JobProgressUpdatedEvent : BaseEvent
{
    public Guid JobId { get; }
    public Guid UserId { get; }
    public int TotalUrls { get; }
    public int CompletedUrls { get; }
    public int FailedUrls { get; }
    public double ProgressPercentage { get; }
    public string? CurrentUrl { get; }
    public DateTime UpdatedAt { get; }

    public JobProgressUpdatedEvent(Guid jobId, Guid userId, int totalUrls, int completedUrls, int failedUrls, double progressPercentage, string? currentUrl, DateTime updatedAt)
    {
        JobId = jobId;
        UserId = userId;
        TotalUrls = totalUrls;
        CompletedUrls = completedUrls;
        FailedUrls = failedUrls;
        ProgressPercentage = progressPercentage;
        CurrentUrl = currentUrl;
        UpdatedAt = updatedAt;
    }
}
