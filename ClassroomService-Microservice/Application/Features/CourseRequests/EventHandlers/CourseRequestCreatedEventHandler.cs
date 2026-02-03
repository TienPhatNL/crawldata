using ClassroomService.Domain.Events;
using ClassroomService.Infrastructure.Messaging;
using MediatR;

namespace ClassroomService.Application.Features.CourseRequests.EventHandlers;

public class CourseRequestCreatedEventHandler : INotificationHandler<CourseRequestCreatedEvent>
{
    private readonly KafkaEventPublisher _eventPublisher;
    private readonly ILogger<CourseRequestCreatedEventHandler> _logger;

    public CourseRequestCreatedEventHandler(
        KafkaEventPublisher eventPublisher,
        ILogger<CourseRequestCreatedEventHandler> logger)
    {
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task Handle(CourseRequestCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "[CourseRequestCreatedEventHandler] Publishing event for course request {CourseRequestId} by {LecturerName}",
                notification.CourseRequestId,
                notification.LecturerName);

            await _eventPublisher.PublishAsync(
                "classroom-events",
                notification.CourseRequestId.ToString(),
                notification,
                cancellationToken);

            _logger.LogInformation(
                "[CourseRequestCreatedEventHandler] Successfully published CourseRequestCreatedEvent to Kafka");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[CourseRequestCreatedEventHandler] Error publishing CourseRequestCreatedEvent for request {CourseRequestId}",
                notification.CourseRequestId);
            throw;
        }
    }
}
