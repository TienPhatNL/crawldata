using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Domain.Events;
using UserService.Infrastructure.Messaging;

namespace UserService.Application.Features.Users.EventHandlers;

public class UserProfileUpdatedEventHandler : INotificationHandler<UserProfileUpdatedEvent>
{
    private readonly ILogger<UserProfileUpdatedEventHandler> _logger;
    private readonly KafkaEventPublisher _eventPublisher;

    public UserProfileUpdatedEventHandler(
        ILogger<UserProfileUpdatedEventHandler> logger,
        KafkaEventPublisher eventPublisher)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(UserProfileUpdatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Profile updated for user {UserId}. Changed fields: {ChangedFields}", 
            notification.UserId, string.Join(", ", notification.ChangedFields));

        try
        {
            await _eventPublisher.PublishEventAsync(
                "user-events",
                notification,
                cancellationToken);
            
            _logger.LogDebug("UserProfileUpdatedEvent published to Kafka for user {UserId}", notification.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish UserProfileUpdatedEvent to Kafka for user {UserId}", notification.UserId);
            throw;
        }
    }
}
