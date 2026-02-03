using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Domain.Events;
using UserService.Infrastructure.Messaging;

namespace UserService.Application.Features.Authentication.EventHandlers;

public class UserEmailConfirmedEventHandler : INotificationHandler<UserEmailConfirmedEvent>
{
    private readonly ILogger<UserEmailConfirmedEventHandler> _logger;
    private readonly KafkaEventPublisher _eventPublisher;

    public UserEmailConfirmedEventHandler(
        ILogger<UserEmailConfirmedEventHandler> logger,
        KafkaEventPublisher eventPublisher)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(UserEmailConfirmedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Email confirmed for user {UserId}: {Email}", 
            notification.UserId, notification.Email);

        try
        {
            await _eventPublisher.PublishEventAsync(
                "user-events",
                notification,
                cancellationToken);
            
            _logger.LogDebug("UserEmailConfirmedEvent published to Kafka for user {UserId}", notification.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish UserEmailConfirmedEvent to Kafka for user {UserId}", notification.UserId);
            throw;
        }
    }
}
