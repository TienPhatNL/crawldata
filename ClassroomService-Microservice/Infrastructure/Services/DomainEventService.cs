using MediatR;
using ClassroomService.Domain.Common;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Infrastructure.Services;

/// <summary>
/// Service for publishing domain events using MediatR
/// </summary>
public class DomainEventService : IDomainEventService
{
    private readonly IPublisher _mediator;

    public DomainEventService(IPublisher mediator)
    {
        _mediator = mediator;
    }

    public async Task PublishAsync(BaseEvent domainEvent, CancellationToken cancellationToken = default)
    {
        await _mediator.Publish(domainEvent, cancellationToken);
    }
}
