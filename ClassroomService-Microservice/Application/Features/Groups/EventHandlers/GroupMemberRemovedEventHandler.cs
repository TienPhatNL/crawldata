using MediatR;
using ClassroomService.Domain.Events;
using ClassroomService.Infrastructure.Messaging;

namespace ClassroomService.Application.Features.Groups.EventHandlers;

/// <summary>
/// Handles GroupMemberRemovedEvent - logs member removal and publishes to Kafka
/// </summary>
public class GroupMemberRemovedEventHandler : INotificationHandler<GroupMemberRemovedEvent>
{
    private readonly ILogger<GroupMemberRemovedEventHandler> _logger;
    private readonly KafkaEventPublisher _eventPublisher;

    public GroupMemberRemovedEventHandler(ILogger<GroupMemberRemovedEventHandler> logger, KafkaEventPublisher eventPublisher)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(GroupMemberRemovedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Member removed from group: Student {StudentId} removed from group {GroupId} ({GroupName}). WasLeader: {WasLeader}, RemovedBy: {RemovedBy}",
            notification.StudentId,
            notification.GroupId,
            notification.GroupName,
            notification.WasLeader,
            notification.RemovedBy);

        // Publish to Kafka for NotificationService
        try
        {
            await _eventPublisher.PublishAsync(
                "classroom-events",
                notification.GroupId.ToString(),
                notification,
                cancellationToken);
            
            _logger.LogDebug("GroupMemberRemovedEvent published to Kafka for group {GroupId}", notification.GroupId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish GroupMemberRemovedEvent to Kafka for group {GroupId}", notification.GroupId);
            throw;
        }
    }
}
