using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Domain.Events;
using UserService.Infrastructure.Messaging;

namespace UserService.Application.Features.ApiKeys.EventHandlers;

public class UserApiKeyRevokedEventHandler : INotificationHandler<UserApiKeyRevokedEvent>
{
    private readonly ILogger<UserApiKeyRevokedEventHandler> _logger;
    private readonly KafkaEventPublisher _eventPublisher;

    public UserApiKeyRevokedEventHandler(
        ILogger<UserApiKeyRevokedEventHandler> logger,
        KafkaEventPublisher eventPublisher)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(UserApiKeyRevokedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("API key revoked for user {UserId}: {ApiKeyName}", 
            notification.UserId, notification.ApiKeyName);

        try
        {
            await _eventPublisher.PublishEventAsync(
                "user-events",
                notification,
                cancellationToken);
            
            _logger.LogDebug("UserApiKeyRevokedEvent published to Kafka for user {UserId}", notification.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish UserApiKeyRevokedEvent to Kafka for user {UserId}", notification.UserId);
            throw;
        }
    }
}
