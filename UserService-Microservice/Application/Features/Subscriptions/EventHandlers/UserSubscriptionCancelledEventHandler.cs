using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Domain.Events;
using UserService.Infrastructure.Messaging;

namespace UserService.Application.Features.Subscriptions.EventHandlers;

public class UserSubscriptionCancelledEventHandler : INotificationHandler<UserSubscriptionCancelledEvent>
{
    private readonly ILogger<UserSubscriptionCancelledEventHandler> _logger;
    private readonly KafkaEventPublisher _eventPublisher;

    public UserSubscriptionCancelledEventHandler(
        ILogger<UserSubscriptionCancelledEventHandler> logger,
        KafkaEventPublisher eventPublisher)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(UserSubscriptionCancelledEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Subscription cancelled for user {UserId}. Previous tier: {PreviousTier}", 
            notification.UserId, notification.PreviousTier);

        try
        {
            await _eventPublisher.PublishEventAsync(
                "user-events",
                notification,
                cancellationToken);
            
            _logger.LogDebug("UserSubscriptionCancelledEvent published to Kafka for user {UserId}", notification.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish UserSubscriptionCancelledEvent to Kafka for user {UserId}", notification.UserId);
            throw;
        }
    }
}
