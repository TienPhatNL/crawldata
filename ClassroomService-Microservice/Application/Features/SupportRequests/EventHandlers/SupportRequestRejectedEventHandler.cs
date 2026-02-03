using MediatR;
using ClassroomService.Domain.Events;
using ClassroomService.Infrastructure.Messaging;

namespace ClassroomService.Application.Features.SupportRequests.EventHandlers;

public class SupportRequestRejectedEventHandler : INotificationHandler<SupportRequestRejectedEvent>
{
    private readonly KafkaEventPublisher _eventPublisher;
    private readonly ILogger<SupportRequestRejectedEventHandler> _logger;

    public SupportRequestRejectedEventHandler(
        KafkaEventPublisher eventPublisher,
        ILogger<SupportRequestRejectedEventHandler> logger)
    {
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task Handle(SupportRequestRejectedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            await _eventPublisher.PublishAsync(
                "classroom-events",
                notification.SupportRequestId.ToString(),
                notification,
                cancellationToken);

            _logger.LogInformation("Published SupportRequestRejectedEvent for request {SupportRequestId}",
                notification.SupportRequestId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing SupportRequestRejectedEvent for request {SupportRequestId}",
                notification.SupportRequestId);
            throw;
        }
    }
}
