using NotificationService.Domain.Common;

namespace NotificationService.Domain.Interfaces;

public interface IDomainEventService
{
    Task PublishAsync(BaseEvent @event, CancellationToken cancellationToken = default);
}
