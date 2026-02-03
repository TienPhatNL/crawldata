using ClassroomService.Domain.Events;
using ClassroomService.Infrastructure.Messaging;
using MediatR;

namespace ClassroomService.Application.Features.SupportRequests.EventHandlers;

public class SupportRequestAcceptedEventHandler : INotificationHandler<SupportRequestAcceptedEvent>
{
    private readonly KafkaEventPublisher _eventPublisher;
    private readonly ILogger<SupportRequestAcceptedEventHandler> _logger;

    public SupportRequestAcceptedEventHandler(
        KafkaEventPublisher eventPublisher,
        ILogger<SupportRequestAcceptedEventHandler> logger)
    {
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task Handle(SupportRequestAcceptedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "[SupportRequestAcceptedEventHandler] Publishing event for support request {SupportRequestId} accepted by {StaffName}",
                notification.SupportRequestId,
                notification.StaffName);

            await _eventPublisher.PublishAsync(
                "classroom-events",
                notification.SupportRequestId.ToString(),
                notification,
                cancellationToken);

            _logger.LogInformation(
                "[SupportRequestAcceptedEventHandler] Successfully published SupportRequestAcceptedEvent to Kafka");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[SupportRequestAcceptedEventHandler] Error publishing SupportRequestAcceptedEvent for request {SupportRequestId}",
                notification.SupportRequestId);
            throw;
        }
    }
}
