using ClassroomService.Domain.Common;

namespace ClassroomService.Domain.Interfaces;

/// <summary>
/// Service for publishing domain events
/// </summary>
public interface IDomainEventService
{
    /// <summary>
    /// Publish a domain event
    /// </summary>
    Task PublishAsync(BaseEvent domainEvent, CancellationToken cancellationToken = default);
}
