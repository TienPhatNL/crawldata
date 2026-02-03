using ClassroomService.Domain.Events;
using ClassroomService.Infrastructure.Messaging;
using MediatR;

namespace ClassroomService.Application.Features.SupportRequests.EventHandlers;

public class SupportRequestResolvedEventHandler : INotificationHandler<SupportRequestResolvedEvent>
{
    private readonly KafkaEventPublisher _eventPublisher;
    private readonly ILogger<SupportRequestResolvedEventHandler> _logger;

    public SupportRequestResolvedEventHandler(
        KafkaEventPublisher eventPublisher,
        ILogger<SupportRequestResolvedEventHandler> logger)
    {
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task Handle(SupportRequestResolvedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "[SupportRequestResolvedEventHandler] Publishing event for support request {SupportRequestId} resolved",
                notification.SupportRequestId);

            await _eventPublisher.PublishAsync(
                "classroom-events",
                notification.SupportRequestId.ToString(),
                notification,
                cancellationToken);

            _logger.LogInformation(
                "[SupportRequestResolvedEventHandler] Successfully published SupportRequestResolvedEvent to Kafka");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[SupportRequestResolvedEventHandler] Error publishing SupportRequestResolvedEvent for request {SupportRequestId}",
                notification.SupportRequestId);
            throw;
        }
    }
}
