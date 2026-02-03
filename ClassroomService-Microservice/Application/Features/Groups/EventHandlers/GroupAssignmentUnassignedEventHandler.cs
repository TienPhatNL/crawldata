using MediatR;
using ClassroomService.Domain.Events;
using ClassroomService.Infrastructure.Messaging;

namespace ClassroomService.Application.Features.Groups.EventHandlers;

/// <summary>
/// Handles GroupAssignmentUnassignedEvent - logs assignment unassignment from group and publishes to Kafka
/// </summary>
public class GroupAssignmentUnassignedEventHandler : INotificationHandler<GroupAssignmentUnassignedEvent>
{
    private readonly ILogger<GroupAssignmentUnassignedEventHandler> _logger;
    private readonly KafkaEventPublisher _eventPublisher;

    public GroupAssignmentUnassignedEventHandler(ILogger<GroupAssignmentUnassignedEventHandler> logger, KafkaEventPublisher eventPublisher)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(GroupAssignmentUnassignedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Assignment unassigned from group: Assignment {AssignmentId} unassigned from group {GroupId}. UnassignedBy: {UnassignedBy}",
            notification.AssignmentId,
            notification.GroupId,
            notification.UnassignedBy);

        // Publish to Kafka for NotificationService
        try
        {
            await _eventPublisher.PublishAsync(
                "classroom-events",
                notification.GroupId.ToString(),
                notification,
                cancellationToken);
            
            _logger.LogDebug("GroupAssignmentUnassignedEvent published to Kafka for group {GroupId}", notification.GroupId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish GroupAssignmentUnassignedEvent to Kafka for group {GroupId}", notification.GroupId);
            throw;
        }
    }
}
