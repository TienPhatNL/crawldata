using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Domain.Events;
using UserService.Infrastructure.Messaging;

namespace UserService.Application.Features.Users.EventHandlers;

public class UserSuspendedEventHandler : INotificationHandler<UserSuspendedEvent>
{
    private readonly ILogger<UserSuspendedEventHandler> _logger;
    private readonly KafkaEventPublisher _eventPublisher;

    public UserSuspendedEventHandler(
        ILogger<UserSuspendedEventHandler> logger,
        KafkaEventPublisher eventPublisher)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(UserSuspendedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {UserId} suspended. Reason: {SuspensionReason}", 
            notification.UserId, notification.SuspensionReason);

        try
        {
            await _eventPublisher.PublishEventAsync(
                "user-events",
                notification,
                cancellationToken);
            
            _logger.LogDebug("UserSuspendedEvent published to Kafka for user {UserId}", notification.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish UserSuspendedEvent to Kafka for user {UserId}", notification.UserId);
            throw;
        }
    }
}
