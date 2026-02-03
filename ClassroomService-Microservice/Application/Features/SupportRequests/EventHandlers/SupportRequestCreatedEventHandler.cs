using ClassroomService.Domain.Events;
using ClassroomService.Infrastructure.Messaging;
using MediatR;

namespace ClassroomService.Application.Features.SupportRequests.EventHandlers;

public class SupportRequestCreatedEventHandler : INotificationHandler<SupportRequestCreatedEvent>
{
    private readonly KafkaEventPublisher _eventPublisher;
    private readonly ILogger<SupportRequestCreatedEventHandler> _logger;

    public SupportRequestCreatedEventHandler(
        KafkaEventPublisher eventPublisher,
        ILogger<SupportRequestCreatedEventHandler> logger)
    {
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task Handle(SupportRequestCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "[SupportRequestCreatedEventHandler] Publishing event for support request {SupportRequestId} by {RequesterName}",
                notification.SupportRequestId,
                notification.RequesterName);

            await _eventPublisher.PublishAsync(
                "classroom-events",
                notification.SupportRequestId.ToString(),
                notification,
                cancellationToken);

            _logger.LogInformation(
                "[SupportRequestCreatedEventHandler] Successfully published SupportRequestCreatedEvent to Kafka");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[SupportRequestCreatedEventHandler] Error publishing SupportRequestCreatedEvent for request {SupportRequestId}",
                notification.SupportRequestId);
            throw;
        }
    }
}
