using MediatR;
using ClassroomService.Domain.Events;
using ClassroomService.Infrastructure.Messaging;

namespace ClassroomService.Application.Features.Courses.EventHandlers;

/// <summary>
/// Handles CourseCreatedEvent - logs course creation and publishes to Kafka
/// </summary>
public class CourseCreatedEventHandler : INotificationHandler<CourseCreatedEvent>
{
    private readonly ILogger<CourseCreatedEventHandler> _logger;
    private readonly KafkaEventPublisher _eventPublisher;

    public CourseCreatedEventHandler(ILogger<CourseCreatedEventHandler> logger, KafkaEventPublisher eventPublisher)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(CourseCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Course created: {CourseId} - {CourseName} ({CourseCode}) by lecturer {LecturerId}",
            notification.CourseId,
            notification.CourseName,
            notification.CourseCode,
            notification.LecturerId);

        // Publish to Kafka for NotificationService
        try
        {
            await _eventPublisher.PublishAsync(
                "classroom-events",
                notification.CourseId.ToString(),
                notification,
                cancellationToken);
            
            _logger.LogDebug("CourseCreatedEvent published to Kafka for course {CourseId}", notification.CourseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish CourseCreatedEvent to Kafka for course {CourseId}", notification.CourseId);
            throw;
        }
    }
}
