using MediatR;
using ClassroomService.Domain.Events;
using ClassroomService.Infrastructure.Messaging;

namespace ClassroomService.Application.Features.Groups.EventHandlers;

/// <summary>
/// Handles GroupCreatedEvent - logs group creation and publishes to Kafka
/// </summary>
public class GroupCreatedEventHandler : INotificationHandler<GroupCreatedEvent>
{
    private readonly ILogger<GroupCreatedEventHandler> _logger;
    private readonly KafkaEventPublisher _eventPublisher;

    public GroupCreatedEventHandler(ILogger<GroupCreatedEventHandler> logger, KafkaEventPublisher eventPublisher)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(GroupCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Group created: {GroupId} - {GroupName} in course {CourseId} by {CreatedBy}. Members: {MemberCount}",
            notification.GroupId,
            notification.GroupName,
            notification.CourseId,
            notification.CreatedBy,
            notification.GroupMemberIds.Count);

        // Publish to Kafka for NotificationService
        try
        {
            await _eventPublisher.PublishAsync(
                "classroom-events",
                notification.GroupId.ToString(),
                notification,
                cancellationToken);
            
            _logger.LogDebug("GroupCreatedEvent published to Kafka for group {GroupId}", notification.GroupId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish GroupCreatedEvent to Kafka for group {GroupId}", notification.GroupId);
            throw;
        }
    }
}
