using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;

namespace NotificationService.Domain.Interfaces;

public interface INotificationTemplateRepository : IRepository<NotificationTemplate>
{
    Task<NotificationTemplate?> GetByTemplateKeyAsync(string templateKey, CancellationToken cancellationToken = default);
    Task<NotificationTemplate?> GetByTypeAsync(NotificationType type, CancellationToken cancellationToken = default);
}
