using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace NotificationService.Infrastructure.Services;

public class NotificationDeliveryService : INotificationDeliveryService
{
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<NotificationDeliveryService> _logger;

    public NotificationDeliveryService(
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        ILogger<NotificationDeliveryService> logger)
    {
        _emailService = emailService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> DeliverAsync(Notification notification, NotificationChannel channel, CancellationToken cancellationToken = default)
    {
        var deliveryLog = new NotificationDeliveryLog
        {
            NotificationId = notification.Id,
            Channel = channel,
            AttemptedAt = DateTime.UtcNow
        };

        try
        {
            switch (channel)
            {
                case NotificationChannel.InApp:
                    // In-app notifications are delivered via SignalR
                    // The notification is already persisted in the database
                    // SignalR will push it to connected clients
                    deliveryLog.Status = DeliveryStatus.Delivered;
                    deliveryLog.DeliveredAt = DateTime.UtcNow;
                    break;

                case NotificationChannel.Email:
                    await DeliverEmailAsync(notification, deliveryLog, cancellationToken);
                    break;

                default:
                    throw new NotSupportedException($"Channel {channel} is not supported");
            }

            await _unitOfWork.DeliveryLogs.AddAsync(deliveryLog, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return deliveryLog.Status == DeliveryStatus.Delivered;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deliver notification {NotificationId} via {Channel}", 
                notification.Id, channel);
            
            deliveryLog.Status = DeliveryStatus.Failed;
            deliveryLog.ErrorMessage = ex.Message;
            
            await _unitOfWork.DeliveryLogs.AddAsync(deliveryLog, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return false;
        }
    }

    private async Task DeliverEmailAsync(Notification notification, NotificationDeliveryLog deliveryLog, CancellationToken cancellationToken)
    {
        // Get user's email from cache or user service
        // For now, we'll assume email is provided in metadata
        var email = notification.MetadataJson; // This should be replaced with actual user email retrieval
        
        if (string.IsNullOrEmpty(email))
        {
            throw new InvalidOperationException("User email not found");
        }

        await _emailService.SendEmailAsync(email, notification.Title, notification.Content);
        
        deliveryLog.Status = DeliveryStatus.Delivered;
        deliveryLog.DeliveredAt = DateTime.UtcNow;
    }

    public async Task<bool> RetryDeliveryAsync(Guid notificationId, NotificationChannel channel, CancellationToken cancellationToken = default)
    {
        var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId, cancellationToken);
        
        if (notification == null)
        {
            _logger.LogWarning("Notification {NotificationId} not found for retry", notificationId);
            return false;
        }

        return await DeliverAsync(notification, channel, cancellationToken);
    }
}
