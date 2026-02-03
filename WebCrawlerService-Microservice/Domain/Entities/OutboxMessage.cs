using WebCrawlerService.Domain.Common;

namespace WebCrawlerService.Domain.Entities;

public class OutboxMessage : BaseAuditableEntity
{
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime OccurredOnUtc { get; set; }
    public DateTime? ProcessedOnUtc { get; set; }
    public string? Error { get; set; }
    public int RetryCount { get; set; } = 0;
    public int MaxRetries { get; set; } = 3;
    public DateTime? NextRetryAtUtc { get; set; }
}