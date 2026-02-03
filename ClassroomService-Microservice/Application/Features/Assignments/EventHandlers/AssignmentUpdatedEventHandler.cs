using MediatR;
using ClassroomService.Domain.Events;
using ClassroomService.Infrastructure.Messaging;

namespace ClassroomService.Application.Features.Assignments.EventHandlers;

/// <summary>
/// Handles AssignmentUpdatedEvent - logs assignment update and publishes to Kafka
/// </summary>
public class AssignmentUpdatedEventHandler : INotificationHandler<AssignmentUpdatedEvent>
{
    private readonly ILogger<AssignmentUpdatedEventHandler> _logger;
    private readonly KafkaEventPublisher _eventPublisher;

    public AssignmentUpdatedEventHandler(ILogger<AssignmentUpdatedEventHandler> logger, KafkaEventPublisher eventPublisher)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(AssignmentUpdatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Assignment updated: {AssignmentId} - {Title} in course {CourseId}. UpdatedAt: {UpdatedAt}, Lecturer: {LecturerId}",
            notification.AssignmentId,
            notification.Title,
            notification.CourseId,
            notification.UpdatedAt,
            notification.LecturerId);

        // Publish to Kafka for NotificationService
        try
        {
            await _eventPublisher.PublishAsync(
                "classroom-events",
                notification.AssignmentId.ToString(),
                notification,
                cancellationToken);
            
            _logger.LogDebug("AssignmentUpdatedEvent published to Kafka for assignment {AssignmentId}", notification.AssignmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish AssignmentUpdatedEvent to Kafka for assignment {AssignmentId}", notification.AssignmentId);
            throw;
        }
    }
}
