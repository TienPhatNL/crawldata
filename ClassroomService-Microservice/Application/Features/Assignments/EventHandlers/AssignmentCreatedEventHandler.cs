using MediatR;
using ClassroomService.Domain.Events;
using ClassroomService.Infrastructure.Messaging;

namespace ClassroomService.Application.Features.Assignments.EventHandlers;

/// <summary>
/// Handles AssignmentCreatedEvent - logs assignment creation and publishes to Kafka
/// </summary>
public class AssignmentCreatedEventHandler : INotificationHandler<AssignmentCreatedEvent>
{
    private readonly ILogger<AssignmentCreatedEventHandler> _logger;
    private readonly KafkaEventPublisher _eventPublisher;

    public AssignmentCreatedEventHandler(ILogger<AssignmentCreatedEventHandler> logger, KafkaEventPublisher eventPublisher)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(AssignmentCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Assignment created: {AssignmentId} - {Title} in course {CourseId}. Due: {DueDate}, Lecturer: {LecturerId}",
            notification.AssignmentId,
            notification.Title,
            notification.CourseId,
            notification.DueDate,
            notification.LecturerId);

        // Publish to Kafka for NotificationService
        try
        {
            await _eventPublisher.PublishAsync(
                "classroom-events",
                notification.AssignmentId.ToString(),
                notification,
                cancellationToken);
            
            _logger.LogDebug("AssignmentCreatedEvent published to Kafka for assignment {AssignmentId}", notification.AssignmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish AssignmentCreatedEvent to Kafka for assignment {AssignmentId}", notification.AssignmentId);
            throw;
        }
    }
}
