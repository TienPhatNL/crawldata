using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;
using NotificationService.Infrastructure.Persistence;

namespace NotificationService.Infrastructure.Repositories;

public class NotificationDeliveryLogRepository : Repository<NotificationDeliveryLog>, INotificationDeliveryLogRepository
{
    public NotificationDeliveryLogRepository(NotificationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<NotificationDeliveryLog>> GetByNotificationIdAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(l => l.NotificationId == notificationId)
            .OrderByDescending(l => l.AttemptedAt)
            .ToListAsync(cancellationToken);
    }
}
