using WebCrawlerService.Domain.Common;

namespace WebCrawlerService.Domain.Interfaces;

public interface IOutboxService
{
    Task AddOutboxMessageAsync<T>(T domainEvent, CancellationToken cancellationToken = default) where T : BaseEvent;
    Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<OutboxMessageDto>> GetUnprocessedMessagesAsync(int batchSize = 50, CancellationToken cancellationToken = default);
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default);
}

public class OutboxMessageDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime OccurredOnUtc { get; set; }
    public int RetryCount { get; set; }
}