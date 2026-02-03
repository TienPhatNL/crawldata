using NotificationService.Domain.Entities;

namespace NotificationService.Domain.Interfaces;

/// <summary>
/// Unit of Work pattern for managing transactions and repository access
/// </summary>
public interface IUnitOfWork : IDisposable
{
    INotificationRepository Notifications { get; }
    INotificationTemplateRepository Templates { get; }
    INotificationDeliveryLogRepository DeliveryLogs { get; }
    IRepository<T> Repository<T>() where T : Domain.Common.BaseEntity;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task ExecuteTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default);
    Task<T> ExecuteTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);
}
