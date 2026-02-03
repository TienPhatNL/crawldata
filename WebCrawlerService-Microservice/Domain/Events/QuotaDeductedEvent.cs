using WebCrawlerService.Domain.Common;

namespace WebCrawlerService.Domain.Events;

/// <summary>
/// Event published when a user's crawl quota is deducted
/// </summary>
public class QuotaDeductedEvent : BaseEvent
{
    public Guid UserId { get; set; }
    public int Amount { get; set; }
    public DateTime Timestamp { get; set; }
    public string Source { get; set; } = "WebCrawlerService";
    public Guid? JobId { get; set; }
}
