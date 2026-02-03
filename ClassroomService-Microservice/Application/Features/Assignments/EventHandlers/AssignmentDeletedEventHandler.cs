using MediatR;
using ClassroomService.Domain.Events;
using ClassroomService.Infrastructure.Messaging;

namespace ClassroomService.Application.Features.Assignments.EventHandlers;

/// <summary>
/// Handles AssignmentDeletedEvent - logs assignment deletion and publishes to Kafka
/// </summary>
public class AssignmentDeletedEventHandler : INotificationHandler<AssignmentDeletedEvent>
{
    private readonly ILogger<AssignmentDeletedEventHandler> _logger;
    private readonly KafkaEventPublisher _eventPublisher;

    public AssignmentDeletedEventHandler(ILogger<AssignmentDeletedEventHandler> logger, KafkaEventPublisher eventPublisher)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(AssignmentDeletedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Assignment deleted: {AssignmentId} - {Title} from course {CourseId}. Groups unassigned: {GroupsUnassigned}, Lecturer: {LecturerId}",
            notification.AssignmentId,
            notification.Title,
            notification.CourseId,
            notification.GroupsUnassigned,
            notification.LecturerId);

        // Publish to Kafka for NotificationService
        try
        {
            await _eventPublisher.PublishAsync(
                "classroom-events",
                notification.AssignmentId.ToString(),
                notification,
                cancellationToken);
            
            _logger.LogDebug("AssignmentDeletedEvent published to Kafka for assignment {AssignmentId}", notification.AssignmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish AssignmentDeletedEvent to Kafka for assignment {AssignmentId}", notification.AssignmentId);
            throw;
        }
    }
}
