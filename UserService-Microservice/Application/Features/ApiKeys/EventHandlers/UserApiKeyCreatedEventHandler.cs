using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Domain.Events;
using UserService.Infrastructure.Messaging;

namespace UserService.Application.Features.ApiKeys.EventHandlers;

public class UserApiKeyCreatedEventHandler : INotificationHandler<UserApiKeyCreatedEvent>
{
    private readonly ILogger<UserApiKeyCreatedEventHandler> _logger;
    private readonly KafkaEventPublisher _eventPublisher;

    public UserApiKeyCreatedEventHandler(
        ILogger<UserApiKeyCreatedEventHandler> logger,
        KafkaEventPublisher eventPublisher)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(UserApiKeyCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("API key created for user {UserId}: {KeyName}", 
            notification.UserId, notification.KeyName);

        try
        {
            await _eventPublisher.PublishEventAsync(
                "user-events",
                notification,
                cancellationToken);
            
            _logger.LogDebug("UserApiKeyCreatedEvent published to Kafka for user {UserId}", notification.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish UserApiKeyCreatedEvent to Kafka for user {UserId}", notification.UserId);
            throw;
        }
    }
}
