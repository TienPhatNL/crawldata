using NotificationService.Domain.Entities;

namespace NotificationService.Domain.Interfaces;

public interface INotificationDeliveryLogRepository : IRepository<NotificationDeliveryLog>
{
    Task<IEnumerable<NotificationDeliveryLog>> GetByNotificationIdAsync(Guid notificationId, CancellationToken cancellationToken = default);
}
