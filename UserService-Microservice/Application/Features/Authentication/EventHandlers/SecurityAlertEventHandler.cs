using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Domain.Events;
using UserService.Infrastructure.Messaging;

namespace UserService.Application.Features.Authentication.EventHandlers;

public class SecurityAlertEventHandler : INotificationHandler<SecurityAlertEvent>
{
    private readonly ILogger<SecurityAlertEventHandler> _logger;
    private readonly KafkaEventPublisher _eventPublisher;

    public SecurityAlertEventHandler(
        ILogger<SecurityAlertEventHandler> logger,
        KafkaEventPublisher eventPublisher)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(SecurityAlertEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Security alert for user {UserId}: {AlertType} - {Details}", 
            notification.UserId, notification.AlertType, notification.Details);

        try
        {
            await _eventPublisher.PublishEventAsync(
                "user-events",
                notification,
                cancellationToken);
            
            _logger.LogDebug("SecurityAlertEvent published to Kafka for user {UserId}", notification.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish SecurityAlertEvent to Kafka for user {UserId}", notification.UserId);
            throw;
        }
    }
}
