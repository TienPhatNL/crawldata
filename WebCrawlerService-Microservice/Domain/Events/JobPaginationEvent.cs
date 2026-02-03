using WebCrawlerService.Domain.Common;

namespace WebCrawlerService.Domain.Events;

/// <summary>
/// Published when pagination events occur during intelligent crawling
/// </summary>
public class JobPaginationEvent : BaseEvent
{
    public Guid JobId { get; }
    public Guid UserId { get; }
    public int PageNumber { get; }
    public int TotalPagesCollected { get; }
    public int? MaxPages { get; }
    public long PageContentSize { get; } // Size in characters/bytes
    public string PageUrl { get; }
    public DateTime OccurredAt { get; }

    public JobPaginationEvent(
        Guid jobId,
        Guid userId,
        int pageNumber,
        int totalPagesCollected,
        long pageContentSize,
        string pageUrl,
        int? maxPages = null,
        DateTime? occurredAt = null)
    {
        JobId = jobId;
        UserId = userId;
        PageNumber = pageNumber;
        TotalPagesCollected = totalPagesCollected;
        MaxPages = maxPages;
        PageContentSize = pageContentSize;
        PageUrl = pageUrl;
        OccurredAt = occurredAt ?? DateTime.UtcNow;
    }
}
