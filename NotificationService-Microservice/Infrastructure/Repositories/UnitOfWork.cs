using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NotificationService.Domain.Common;
using NotificationService.Domain.Interfaces;
using NotificationService.Infrastructure.Persistence;

namespace NotificationService.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly NotificationDbContext _context;
    private INotificationRepository? _notifications;
    private INotificationTemplateRepository? _templates;
    private INotificationDeliveryLogRepository? _deliveryLogs;
    private readonly Dictionary<Type, object> _repositories = new();

    public UnitOfWork(NotificationDbContext context)
    {
        _context = context;
    }

    public INotificationRepository Notifications =>
        _notifications ??= new NotificationRepository(_context);

    public INotificationTemplateRepository Templates =>
        _templates ??= new NotificationTemplateRepository(_context);

    public INotificationDeliveryLogRepository DeliveryLogs =>
        _deliveryLogs ??= new NotificationDeliveryLogRepository(_context);

    public IRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity
    {
        var type = typeof(TEntity);
        
        if (_repositories.ContainsKey(type))
        {
            return (IRepository<TEntity>)_repositories[type];
        }

        var repositoryType = typeof(Repository<>).MakeGenericType(type);
        var repository = (IRepository<TEntity>)Activator.CreateInstance(repositoryType, _context)!;
        _repositories[type] = repository;

        return repository;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ExecuteTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await operation();
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public async Task<T> ExecuteTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await operation();
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
