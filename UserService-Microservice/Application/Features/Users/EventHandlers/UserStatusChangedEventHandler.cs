using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Domain.Events;
using UserService.Infrastructure.Messaging;

namespace UserService.Application.Features.Users.EventHandlers;

public class UserStatusChangedEventHandler : INotificationHandler<UserStatusChangedEvent>
{
    private readonly ILogger<UserStatusChangedEventHandler> _logger;
    private readonly KafkaEventPublisher _eventPublisher;

    public UserStatusChangedEventHandler(
        ILogger<UserStatusChangedEventHandler> logger,
        KafkaEventPublisher eventPublisher)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(UserStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Status changed for user {UserId}: {OldStatus} → {NewStatus}", 
            notification.UserId, notification.OldStatus, notification.NewStatus);

        try
        {
            await _eventPublisher.PublishEventAsync(
                "user-events",
                notification,
                cancellationToken);
            
            _logger.LogDebug("UserStatusChangedEvent published to Kafka for user {UserId}", notification.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish UserStatusChangedEvent to Kafka for user {UserId}", notification.UserId);
            throw;
        }
    }
}
