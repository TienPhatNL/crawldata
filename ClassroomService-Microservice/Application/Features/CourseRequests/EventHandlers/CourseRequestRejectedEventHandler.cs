using ClassroomService.Domain.Events;
using ClassroomService.Infrastructure.Messaging;
using MediatR;

namespace ClassroomService.Application.Features.CourseRequests.EventHandlers;

public class CourseRequestRejectedEventHandler : INotificationHandler<CourseRequestRejectedEvent>
{
    private readonly KafkaEventPublisher _eventPublisher;
    private readonly ILogger<CourseRequestRejectedEventHandler> _logger;

    public CourseRequestRejectedEventHandler(
        KafkaEventPublisher eventPublisher,
        ILogger<CourseRequestRejectedEventHandler> logger)
    {
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task Handle(CourseRequestRejectedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "[CourseRequestRejectedEventHandler] Publishing event for rejected course request {CourseRequestId}",
                notification.CourseRequestId);

            await _eventPublisher.PublishAsync(
                "classroom-events",
                notification.CourseRequestId.ToString(),
                notification,
                cancellationToken);

            _logger.LogInformation(
                "[CourseRequestRejectedEventHandler] Successfully published CourseRequestRejectedEvent to Kafka");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[CourseRequestRejectedEventHandler] Error publishing CourseRequestRejectedEvent for request {CourseRequestId}",
                notification.CourseRequestId);
            throw;
        }
    }
}
