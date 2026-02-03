using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Domain.Events;
using UserService.Infrastructure.Messaging;

namespace UserService.Application.Features.Users.EventHandlers;

public class UserReactivatedEventHandler : INotificationHandler<UserReactivatedEvent>
{
    private readonly ILogger<UserReactivatedEventHandler> _logger;
    private readonly KafkaEventPublisher _eventPublisher;

    public UserReactivatedEventHandler(
        ILogger<UserReactivatedEventHandler> logger,
        KafkaEventPublisher eventPublisher)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(UserReactivatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {UserId} reactivated by {ReactivatedById}", 
            notification.UserId, notification.ReactivatedById);

        try
        {
            await _eventPublisher.PublishEventAsync(
                "user-events",
                notification,
                cancellationToken);
            
            _logger.LogDebug("UserReactivatedEvent published to Kafka for user {UserId}", notification.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish UserReactivatedEvent to Kafka for user {UserId}", notification.UserId);
            throw;
        }
    }
}
