using MediatR;
using ClassroomService.Domain.Events;
using ClassroomService.Infrastructure.Messaging;

namespace ClassroomService.Application.Features.Assignments.EventHandlers;

/// <summary>
/// Handles AssignmentStatusChangedEvent - logs status change and publishes to Kafka
/// </summary>
public class AssignmentStatusChangedEventHandler : INotificationHandler<AssignmentStatusChangedEvent>
{
    private readonly ILogger<AssignmentStatusChangedEventHandler> _logger;
    private readonly KafkaEventPublisher _eventPublisher;

    public AssignmentStatusChangedEventHandler(ILogger<AssignmentStatusChangedEventHandler> logger, KafkaEventPublisher eventPublisher)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(AssignmentStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Assignment status changed: {AssignmentId} - {Title} in course {CourseId}. Old: {OldStatus}, New: {NewStatus}, IsAutomatic: {IsAutomatic}, Lecturer: {LecturerId}",
            notification.AssignmentId,
            notification.Title,
            notification.CourseId,
            notification.OldStatus,
            notification.NewStatus,
            notification.IsAutomatic,
            notification.LecturerId);

        // Publish to Kafka for NotificationService
        try
        {
            await _eventPublisher.PublishAsync(
                "classroom-events",
                notification.AssignmentId.ToString(),
                notification,
                cancellationToken);
            
            _logger.LogDebug("AssignmentStatusChangedEvent published to Kafka for assignment {AssignmentId}", notification.AssignmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish AssignmentStatusChangedEvent to Kafka for assignment {AssignmentId}", notification.AssignmentId);
            throw;
        }
    }
}
