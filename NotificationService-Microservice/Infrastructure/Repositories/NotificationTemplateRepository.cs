using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Interfaces;
using NotificationService.Infrastructure.Persistence;

namespace NotificationService.Infrastructure.Repositories;

public class NotificationTemplateRepository : Repository<NotificationTemplate>, INotificationTemplateRepository
{
    public NotificationTemplateRepository(NotificationDbContext context) : base(context)
    {
    }

    public async Task<NotificationTemplate?> GetByTemplateKeyAsync(string templateKey, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(t => t.TemplateKey == templateKey && t.IsActive, cancellationToken);
    }

    public async Task<NotificationTemplate?> GetByTypeAsync(NotificationType type, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(t => t.Type == type && t.IsActive, cancellationToken);
    }
}
