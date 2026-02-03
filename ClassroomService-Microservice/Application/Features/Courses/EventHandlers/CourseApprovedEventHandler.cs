using MediatR;
using ClassroomService.Domain.Events;
using ClassroomService.Infrastructure.Messaging;

namespace ClassroomService.Application.Features.Courses.EventHandlers;

/// <summary>
/// Handles CourseApprovedEvent - logs course approvals and publishes to Kafka
/// </summary>
public class CourseApprovedEventHandler : INotificationHandler<CourseApprovedEvent>
{
    private readonly ILogger<CourseApprovedEventHandler> _logger;
    private readonly KafkaEventPublisher _eventPublisher;

    public CourseApprovedEventHandler(ILogger<CourseApprovedEventHandler> logger, KafkaEventPublisher eventPublisher)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(CourseApprovedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Course approved: {CourseId} - {CourseName} by staff {ApprovedBy}. Comments: {Comments}",
            notification.CourseId,
            notification.CourseName,
            notification.ApprovedBy,
            notification.Comments ?? "No comments");

        // Publish to Kafka for NotificationService
        try
        {
            await _eventPublisher.PublishAsync(
                "classroom-events",
                notification.CourseId.ToString(),
                notification,
                cancellationToken);
            
            _logger.LogDebug("CourseApprovedEvent published to Kafka for course {CourseId}", notification.CourseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish CourseApprovedEvent to Kafka for course {CourseId}", notification.CourseId);
            throw;
        }
    }
}
