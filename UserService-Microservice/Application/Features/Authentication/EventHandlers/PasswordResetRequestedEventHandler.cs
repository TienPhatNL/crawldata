using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Domain.Events;
using UserService.Infrastructure.Messaging;

namespace UserService.Application.Features.Authentication.EventHandlers;

public class PasswordResetRequestedEventHandler : INotificationHandler<PasswordResetRequestedEvent>
{
    private readonly ILogger<PasswordResetRequestedEventHandler> _logger;
    private readonly KafkaEventPublisher _eventPublisher;

    public PasswordResetRequestedEventHandler(
        ILogger<PasswordResetRequestedEventHandler> logger,
        KafkaEventPublisher eventPublisher)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(PasswordResetRequestedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Password reset requested for user {UserId} with email {Email}", 
            notification.UserId, notification.Email);

        try
        {
            await _eventPublisher.PublishEventAsync(
                "user-events",
                notification,
                cancellationToken);
            
            _logger.LogDebug("PasswordResetRequestedEvent published to Kafka for user {UserId}", notification.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish PasswordResetRequestedEvent to Kafka for user {UserId}", notification.UserId);
            throw;
        }
    }
}
