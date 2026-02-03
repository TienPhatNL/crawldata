using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Domain.Events;
using UserService.Infrastructure.Messaging;

namespace UserService.Application.Features.Subscriptions.EventHandlers;

public class UserSubscriptionUpgradedEventHandler : INotificationHandler<UserSubscriptionUpgradedEvent>
{
    private readonly ILogger<UserSubscriptionUpgradedEventHandler> _logger;
    private readonly KafkaEventPublisher _eventPublisher;

    public UserSubscriptionUpgradedEventHandler(
        ILogger<UserSubscriptionUpgradedEventHandler> logger,
        KafkaEventPublisher eventPublisher)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(UserSubscriptionUpgradedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Subscription upgraded for user {UserId}: {OldTier} → {NewTier}", 
            notification.UserId, notification.OldTier, notification.NewTier);

        try
        {
            await _eventPublisher.PublishEventAsync(
                "user-events",
                notification,
                cancellationToken);
            
            _logger.LogDebug("UserSubscriptionUpgradedEvent published to Kafka for user {UserId}", notification.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish UserSubscriptionUpgradedEvent to Kafka for user {UserId}", notification.UserId);
            throw;
        }
    }
}
