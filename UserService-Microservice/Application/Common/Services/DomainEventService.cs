using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Domain.Common;
using UserService.Domain.Services;

namespace UserService.Application.Common.Services;

public class DomainEventService : IDomainEventService
{
    private readonly IMediator _mediator;
    private readonly ILogger<DomainEventService> _logger;

    public DomainEventService(IMediator mediator, ILogger<DomainEventService> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task PublishAsync(BaseEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Publishing domain event {EventType}", domainEvent.GetType().Name);
            await _mediator.Publish(domainEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish domain event {EventType}", domainEvent.GetType().Name);
            throw;
        }
    }
}