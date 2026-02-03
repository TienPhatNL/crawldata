using MediatR;
using ClassroomService.Domain.Events;
using ClassroomService.Infrastructure.Messaging;

namespace ClassroomService.Application.Features.Assignments.EventHandlers;

/// <summary>
/// Handles AssignmentClosedEvent - logs assignment closure and publishes to Kafka
/// </summary>
public class AssignmentClosedEventHandler : INotificationHandler<AssignmentClosedEvent>
{
    private readonly ILogger<AssignmentClosedEventHandler> _logger;
    private readonly KafkaEventPublisher _eventPublisher;

    public AssignmentClosedEventHandler(ILogger<AssignmentClosedEventHandler> logger, KafkaEventPublisher eventPublisher)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(AssignmentClosedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Assignment closed: {AssignmentId} - {Title} in course {CourseId}. ClosedAt: {ClosedAt}, Enrolled students: {StudentCount}",
            notification.AssignmentId,
            notification.Title,
            notification.CourseId,
            notification.ClosedAt,
            notification.EnrolledStudentIds.Count);

        // Publish to Kafka for NotificationService
        try
        {
            await _eventPublisher.PublishAsync(
                "classroom-events",
                notification.AssignmentId.ToString(),
                notification,
                cancellationToken);
            
            _logger.LogDebug("AssignmentClosedEvent published to Kafka for assignment {AssignmentId}", notification.AssignmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish AssignmentClosedEvent to Kafka for assignment {AssignmentId}", notification.AssignmentId);
            throw;
        }
    }
}
