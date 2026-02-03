using ClassroomService.Domain.Events;
using ClassroomService.Infrastructure.Messaging;
using MediatR;

namespace ClassroomService.Application.Features.SupportRequests.EventHandlers;

public class SupportRequestCancelledEventHandler : INotificationHandler<SupportRequestCancelledEvent>
{
    private readonly KafkaEventPublisher _eventPublisher;
    private readonly ILogger<SupportRequestCancelledEventHandler> _logger;

    public SupportRequestCancelledEventHandler(
        KafkaEventPublisher eventPublisher,
        ILogger<SupportRequestCancelledEventHandler> logger)
    {
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task Handle(SupportRequestCancelledEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "[SupportRequestCancelledEventHandler] Publishing event for support request {SupportRequestId} cancelled by {RequesterName}",
                notification.SupportRequestId,
                notification.RequesterName);

            await _eventPublisher.PublishAsync(
                "classroom-events",
                notification.SupportRequestId.ToString(),
                notification,
                cancellationToken);

            _logger.LogInformation(
                "[SupportRequestCancelledEventHandler] Successfully published SupportRequestCancelledEvent to Kafka");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[SupportRequestCancelledEventHandler] Error publishing SupportRequestCancelledEvent for request {SupportRequestId}",
                notification.SupportRequestId);
            throw;
        }
    }
}
