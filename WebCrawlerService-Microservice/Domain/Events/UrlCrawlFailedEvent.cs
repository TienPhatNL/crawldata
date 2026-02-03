using WebCrawlerService.Domain.Common;

namespace WebCrawlerService.Domain.Events;

/// <summary>
/// Published when a single URL crawl fails
/// </summary>
public class UrlCrawlFailedEvent : BaseEvent
{
    public Guid JobId { get; }
    public Guid UserId { get; }
    public string Url { get; }
    public string ErrorMessage { get; }
    public int RetryCount { get; }
    public bool WillRetry { get; }
    public DateTime FailedAt { get; }

    public UrlCrawlFailedEvent(Guid jobId, Guid userId, string url, string errorMessage, int retryCount, bool willRetry, DateTime failedAt)
    {
        JobId = jobId;
        UserId = userId;
        Url = url;
        ErrorMessage = errorMessage;
        RetryCount = retryCount;
        WillRetry = willRetry;
        FailedAt = failedAt;
    }
}
