using MediatR;
using ClassroomService.Domain.Events;
using ClassroomService.Infrastructure.Messaging;

namespace ClassroomService.Application.Features.Assignments.EventHandlers;

/// <summary>
/// Handles AssignmentDueDateExtendedEvent - logs due date extension and publishes to Kafka
/// </summary>
public class AssignmentDueDateExtendedEventHandler : INotificationHandler<AssignmentDueDateExtendedEvent>
{
    private readonly ILogger<AssignmentDueDateExtendedEventHandler> _logger;
    private readonly KafkaEventPublisher _eventPublisher;

    public AssignmentDueDateExtendedEventHandler(ILogger<AssignmentDueDateExtendedEventHandler> logger, KafkaEventPublisher eventPublisher)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(AssignmentDueDateExtendedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Assignment due date extended: {AssignmentId} - {Title} in course {CourseId}. Original: {OriginalDueDate}, Extended: {ExtendedDueDate}, Enrolled students: {StudentCount}",
            notification.AssignmentId,
            notification.Title,
            notification.CourseId,
            notification.OriginalDueDate,
            notification.ExtendedDueDate,
            notification.EnrolledStudentIds.Count);

        // Publish to Kafka for NotificationService
        try
        {
            await _eventPublisher.PublishAsync(
                "classroom-events",
                notification.AssignmentId.ToString(),
                notification,
                cancellationToken);
            
            _logger.LogDebug("AssignmentDueDateExtendedEvent published to Kafka for assignment {AssignmentId}", notification.AssignmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish AssignmentDueDateExtendedEvent to Kafka for assignment {AssignmentId}", notification.AssignmentId);
            throw;
        }
    }
}
