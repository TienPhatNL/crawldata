using UserService.Domain.Common;

namespace UserService.Domain.Services;

public interface IDomainEventService
{
    Task PublishAsync(BaseEvent domainEvent, CancellationToken cancellationToken = default);
}