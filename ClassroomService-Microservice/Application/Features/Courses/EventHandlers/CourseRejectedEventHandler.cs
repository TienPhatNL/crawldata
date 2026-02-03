using MediatR;
using ClassroomService.Domain.Events;
using ClassroomService.Infrastructure.Messaging;

namespace ClassroomService.Application.Features.Courses.EventHandlers;

/// <summary>
/// Handles CourseRejectedEvent - logs course rejections and publishes to Kafka
/// </summary>
public class CourseRejectedEventHandler : INotificationHandler<CourseRejectedEvent>
{
    private readonly ILogger<CourseRejectedEventHandler> _logger;
    private readonly KafkaEventPublisher _eventPublisher;

    public CourseRejectedEventHandler(ILogger<CourseRejectedEventHandler> logger, KafkaEventPublisher eventPublisher)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(CourseRejectedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Course rejected: {CourseId} - {CourseName} by staff {RejectedBy}. Reason: {RejectionReason}",
            notification.CourseId,
            notification.CourseName,
            notification.RejectedBy,
            notification.RejectionReason ?? "No reason provided");

        // Publish to Kafka for NotificationService
        try
        {
            await _eventPublisher.PublishAsync(
                "classroom-events",
                notification.CourseId.ToString(),
                notification,
                cancellationToken);
            
            _logger.LogDebug("CourseRejectedEvent published to Kafka for course {CourseId}", notification.CourseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish CourseRejectedEvent to Kafka for course {CourseId}", notification.CourseId);
            throw;
        }
    }
}
