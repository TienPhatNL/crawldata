using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;

namespace NotificationService.Domain.Interfaces;

public interface INotificationDeliveryService
{
    Task<bool> DeliverAsync(Notification notification, NotificationChannel channel, CancellationToken cancellationToken = default);
    Task<bool> RetryDeliveryAsync(Guid notificationId, NotificationChannel channel, CancellationToken cancellationToken = default);
}
