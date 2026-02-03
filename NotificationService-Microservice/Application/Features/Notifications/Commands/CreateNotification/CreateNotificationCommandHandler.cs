using MediatR;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace NotificationService.Application.Features.Notifications.Commands.CreateNotification;

public class CreateNotificationCommandHandler : IRequestHandler<CreateNotificationCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDeliveryService _deliveryService;
    private readonly ILogger<CreateNotificationCommandHandler> _logger;

    public CreateNotificationCommandHandler(
        IUnitOfWork unitOfWork,
        INotificationDeliveryService deliveryService,
        ILogger<CreateNotificationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _deliveryService = deliveryService;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateNotificationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Create notification entity
            var notification = new Notification
            {
                UserId = request.UserId,
                Title = request.Title,
                Content = request.Content,
                Type = request.Type,
                Priority = request.Priority,
                Source = request.Source,
                MetadataJson = request.MetadataJson,
                ExpiresAt = request.ExpiresAt,
                IsRead = false
            };

            await _unitOfWork.Notifications.AddAsync(notification, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created notification {NotificationId} for user {UserId}", 
                notification.Id, request.UserId);

            // Deliver notification through all requested channels
            foreach (var channel in request.Channels)
            {
                try
                {
                    await _deliveryService.DeliverAsync(notification, channel, cancellationToken);
                    _logger.LogInformation("Delivered notification {NotificationId} via {Channel}", 
                        notification.Id, channel);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to deliver notification {NotificationId} via {Channel}", 
                        notification.Id, channel);
                    // Continue with other channels even if one fails
                }
            }

            return notification.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating notification for user {UserId}", request.UserId);
            throw;
        }
    }
}
