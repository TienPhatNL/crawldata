using System;

namespace UserService.Infrastructure.Messaging.Contracts;

public class CrawlQuotaUsageEvent
{
    public Guid UserId { get; set; }
    public string? JobId { get; set; }
    public int UnitsConsumed { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime OccurredAt { get; set; }
    public string? Source { get; set; }
}
