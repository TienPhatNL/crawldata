using MediatR;
using ClassroomService.Domain.Events;
using ClassroomService.Infrastructure.Messaging;

namespace ClassroomService.Application.Features.Groups.EventHandlers;

/// <summary>
/// Handles GroupLeaderChangedEvent - logs leader change and publishes to Kafka
/// </summary>
public class GroupLeaderChangedEventHandler : INotificationHandler<GroupLeaderChangedEvent>
{
    private readonly ILogger<GroupLeaderChangedEventHandler> _logger;
    private readonly KafkaEventPublisher _eventPublisher;

    public GroupLeaderChangedEventHandler(ILogger<GroupLeaderChangedEventHandler> logger, KafkaEventPublisher eventPublisher)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(GroupLeaderChangedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Group leader changed: Group {GroupId} ({GroupName}). PreviousLeader: {PreviousLeaderId}, NewLeader: {NewLeaderId}, ChangedBy: {ChangedBy}",
            notification.GroupId,
            notification.GroupName,
            notification.PreviousLeaderId,
            notification.NewLeaderId,
            notification.ChangedBy);

        // Publish to Kafka for NotificationService
        try
        {
            await _eventPublisher.PublishAsync(
                "classroom-events",
                notification.GroupId.ToString(),
                notification,
                cancellationToken);
            
            _logger.LogDebug("GroupLeaderChangedEvent published to Kafka for group {GroupId}", notification.GroupId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish GroupLeaderChangedEvent to Kafka for group {GroupId}", notification.GroupId);
            throw;
        }
    }
}
