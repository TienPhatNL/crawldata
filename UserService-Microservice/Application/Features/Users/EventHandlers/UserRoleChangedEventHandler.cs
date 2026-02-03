using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Domain.Events;
using UserService.Infrastructure.Messaging;

namespace UserService.Application.Features.Users.EventHandlers;

public class UserRoleChangedEventHandler : INotificationHandler<UserRoleChangedEvent>
{
    private readonly ILogger<UserRoleChangedEventHandler> _logger;
    private readonly KafkaEventPublisher _eventPublisher;

    public UserRoleChangedEventHandler(
        ILogger<UserRoleChangedEventHandler> logger,
        KafkaEventPublisher eventPublisher)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(UserRoleChangedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Role changed for user {UserId}: {OldRole} → {NewRole}", 
            notification.UserId, notification.OldRole, notification.NewRole);

        try
        {
            await _eventPublisher.PublishEventAsync(
                "user-events",
                notification,
                cancellationToken);
            
            _logger.LogDebug("UserRoleChangedEvent published to Kafka for user {UserId}", notification.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish UserRoleChangedEvent to Kafka for user {UserId}", notification.UserId);
            throw;
        }
    }
}
