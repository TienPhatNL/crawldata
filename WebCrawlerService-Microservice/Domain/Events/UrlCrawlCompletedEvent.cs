using WebCrawlerService.Domain.Common;

namespace WebCrawlerService.Domain.Events;

/// <summary>
/// Published when a single URL crawl completes successfully
/// </summary>
public class UrlCrawlCompletedEvent : BaseEvent
{
    public Guid JobId { get; }
    public Guid UserId { get; }
    public string Url { get; }
    public int HttpStatusCode { get; }
    public int ResponseTimeMs { get; }
    public int ExtractedItemCount { get; }
    public long ContentSize { get; }
    public DateTime CompletedAt { get; }

    public UrlCrawlCompletedEvent(Guid jobId, Guid userId, string url, int httpStatusCode, int responseTimeMs, int extractedItemCount, long contentSize, DateTime completedAt)
    {
        JobId = jobId;
        UserId = userId;
        Url = url;
        HttpStatusCode = httpStatusCode;
        ResponseTimeMs = responseTimeMs;
        ExtractedItemCount = extractedItemCount;
        ContentSize = contentSize;
        CompletedAt = completedAt;
    }
}
