using MediatR;
using ClassroomService.Domain.Events;
using ClassroomService.Infrastructure.Messaging;

namespace ClassroomService.Application.Features.Groups.EventHandlers;

/// <summary>
/// Handles GroupMemberAddedEvent - logs member addition and publishes to Kafka
/// </summary>
public class GroupMemberAddedEventHandler : INotificationHandler<GroupMemberAddedEvent>
{
    private readonly ILogger<GroupMemberAddedEventHandler> _logger;
    private readonly KafkaEventPublisher _eventPublisher;

    public GroupMemberAddedEventHandler(ILogger<GroupMemberAddedEventHandler> logger, KafkaEventPublisher eventPublisher)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(GroupMemberAddedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Member added to group: Student {StudentId} added to group {GroupId} ({GroupName}). IsLeader: {IsLeader}, AddedBy: {AddedBy}",
            notification.StudentId,
            notification.GroupId,
            notification.GroupName,
            notification.IsLeader,
            notification.AddedBy);

        // Publish to Kafka for NotificationService
        try
        {
            await _eventPublisher.PublishAsync(
                "classroom-events",
                notification.GroupId.ToString(),
                notification,
                cancellationToken);
            
            _logger.LogDebug("GroupMemberAddedEvent published to Kafka for group {GroupId}", notification.GroupId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish GroupMemberAddedEvent to Kafka for group {GroupId}", notification.GroupId);
            throw;
        }
    }
}
