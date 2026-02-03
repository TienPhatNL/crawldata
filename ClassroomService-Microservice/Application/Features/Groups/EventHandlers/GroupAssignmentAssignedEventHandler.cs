using MediatR;
using ClassroomService.Domain.Events;
using ClassroomService.Infrastructure.Messaging;

namespace ClassroomService.Application.Features.Groups.EventHandlers;

/// <summary>
/// Handles GroupAssignmentAssignedEvent - logs assignment assignment to group and publishes to Kafka
/// </summary>
public class GroupAssignmentAssignedEventHandler : INotificationHandler<GroupAssignmentAssignedEvent>
{
    private readonly ILogger<GroupAssignmentAssignedEventHandler> _logger;
    private readonly KafkaEventPublisher _eventPublisher;

    public GroupAssignmentAssignedEventHandler(ILogger<GroupAssignmentAssignedEventHandler> logger, KafkaEventPublisher eventPublisher)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(GroupAssignmentAssignedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Assignment assigned to group: Assignment {AssignmentId} ({AssignmentTitle}) assigned to group {GroupId} ({GroupName}). Status: {AssignmentStatus}, AssignedBy: {AssignedBy}",
            notification.AssignmentId,
            notification.AssignmentTitle,
            notification.GroupId,
            notification.GroupName,
            notification.AssignmentStatus,
            notification.AssignedBy);

        // Publish to Kafka for NotificationService
        try
        {
            await _eventPublisher.PublishAsync(
                "classroom-events",
                notification.GroupId.ToString(),
                notification,
                cancellationToken);
            
            _logger.LogDebug("GroupAssignmentAssignedEvent published to Kafka for group {GroupId}", notification.GroupId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish GroupAssignmentAssignedEvent to Kafka for group {GroupId}", notification.GroupId);
            throw;
        }
    }
}
