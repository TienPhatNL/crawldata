using NotificationService.Domain.Common;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Infrastructure.Services;

public class DomainEventService : IDomainEventService
{
    public async Task PublishAsync(BaseEvent @event, CancellationToken cancellationToken = default)
    {
        // This will be handled by MediatR in the Application layer
        // Domain events are raised but publishing happens after SaveChanges
        // through the Application layer's INotificationHandler implementations
        await Task.CompletedTask;
    }
}
