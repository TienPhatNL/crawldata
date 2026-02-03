using MediatR;
using ClassroomService.Domain.Events;
using ClassroomService.Infrastructure.Messaging;

namespace ClassroomService.Application.Features.Courses.EventHandlers;

/// <summary>
/// Handles CourseStatusChangedEvent - logs status changes and publishes to Kafka
/// </summary>
public class CourseStatusChangedEventHandler : INotificationHandler<CourseStatusChangedEvent>
{
    private readonly ILogger<CourseStatusChangedEventHandler> _logger;
    private readonly KafkaEventPublisher _eventPublisher;

    public CourseStatusChangedEventHandler(ILogger<CourseStatusChangedEventHandler> logger, KafkaEventPublisher eventPublisher)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(CourseStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Course status changed: {CourseId} from {OldStatus} to {NewStatus} by user {ChangedBy}. Reason: {Reason}",
            notification.CourseId,
            notification.OldStatus,
            notification.NewStatus,
            notification.ChangedBy,
            notification.Comments ?? "No comments provided");

        // Publish to Kafka for NotificationService
        try
        {
            await _eventPublisher.PublishAsync(
                "classroom-events",
                notification.CourseId.ToString(),
                notification,
                cancellationToken);
            
            _logger.LogDebug("CourseStatusChangedEvent published to Kafka for course {CourseId}", notification.CourseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish CourseStatusChangedEvent to Kafka for course {CourseId}", notification.CourseId);
            throw;
        }
    }
}
