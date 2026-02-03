using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;
using NotificationService.Infrastructure.Persistence;

namespace NotificationService.Infrastructure.Repositories;

public class NotificationRepository : Repository<Notification>, INotificationRepository
{
    public NotificationRepository(NotificationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(Guid userId, bool? isRead, int take, bool isStaff = false, CancellationToken cancellationToken = default)
    {
        // For staff users, include both personal notifications and group notifications (UserId = Guid.Empty)
        var query = isStaff 
            ? _dbSet.Where(n => (n.UserId == userId || n.UserId == Guid.Empty) && !n.IsDeleted)
            : _dbSet.Where(n => n.UserId == userId && !n.IsDeleted);

        if (isRead.HasValue)
        {
            query = query.Where(n => n.IsRead == isRead.Value);
        }

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, bool isStaff = false, CancellationToken cancellationToken = default)
    {
        // For staff users, include both personal notifications and group notifications
        var query = isStaff
            ? _dbSet.Where(n => (n.UserId == userId || n.UserId == Guid.Empty) && !n.IsRead && !n.IsDeleted)
            : _dbSet.Where(n => n.UserId == userId && !n.IsRead && !n.IsDeleted);
            
        return await query.CountAsync(cancellationToken);
    }

    public async Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await GetByIdAsync(notificationId, cancellationToken);
        if (notification != null && !notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await UpdateAsync(notification, cancellationToken);
        }
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var unreadNotifications = await _dbSet
            .Where(n => n.UserId == userId && !n.IsRead && !n.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }

        await UpdateRangeAsync(unreadNotifications, cancellationToken);
    }
}
