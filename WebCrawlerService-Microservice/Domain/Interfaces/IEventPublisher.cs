using WebCrawlerService.Domain.Common;

namespace WebCrawlerService.Domain.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default) where T : BaseEvent;
    Task PublishAsync(string eventType, string eventContent, CancellationToken cancellationToken = default);
}