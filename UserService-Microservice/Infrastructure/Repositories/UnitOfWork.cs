using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using UserService.Domain.Common;
using UserService.Domain.Entities;
using UserService.Domain.Services;
using UserService.Infrastructure.Persistence;

namespace UserService.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly UserDbContext _context;
    private readonly IDomainEventService _domainEventService;
    private bool _disposed = false;

    // Repository instances
    private IUserRepository? _users;
    private IRepository<UserApiKey>? _userApiKeys;
    private IRepository<UserSubscription>? _userSubscriptions;
    private IRepository<UserUsageRecord>? _userUsageRecords;
    private IRepository<UserPreference>? _userPreferences;
    private IRepository<UserSession>? _userSessions;
    private IRepository<AllowedEmailDomain>? _allowedEmailDomains;
    private IRepository<UserQuotaSnapshot>? _userQuotaSnapshots;
    private IRepository<SubscriptionPayment>? _subscriptionPayments;
    private IRepository<SubscriptionPlan>? _subscriptionPlans;
    private IRepository<Announcement>? _announcements;

    public UnitOfWork(UserDbContext context, IDomainEventService domainEventService)
    {
        _context = context;
        _domainEventService = domainEventService;
    }

    public IUserRepository Users =>
        _users ??= new UserRepository(_context);

    public IRepository<UserApiKey> UserApiKeys =>
        _userApiKeys ??= new Repository<UserApiKey>(_context);

    public IRepository<UserSubscription> UserSubscriptions =>
        _userSubscriptions ??= new Repository<UserSubscription>(_context);

    public IRepository<UserUsageRecord> UserUsageRecords =>
        _userUsageRecords ??= new Repository<UserUsageRecord>(_context);

    public IRepository<UserPreference> UserPreferences =>
        _userPreferences ??= new Repository<UserPreference>(_context);

    public IRepository<UserSession> UserSessions =>
        _userSessions ??= new Repository<UserSession>(_context);
    
    public IRepository<AllowedEmailDomain> AllowedEmailDomains =>
        _allowedEmailDomains ??= new Repository<AllowedEmailDomain>(_context);

    public IRepository<UserQuotaSnapshot> UserQuotaSnapshots =>
        _userQuotaSnapshots ??= new Repository<UserQuotaSnapshot>(_context);

    public IRepository<SubscriptionPayment> SubscriptionPayments =>
        _subscriptionPayments ??= new Repository<SubscriptionPayment>(_context);

    public IRepository<SubscriptionPlan> SubscriptionPlans =>
        _subscriptionPlans ??= new Repository<SubscriptionPlan>(_context);

    public IRepository<Announcement> Announcements =>
        _announcements ??= new Repository<Announcement>(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await DispatchDomainEventsAsync(cancellationToken);
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
                await DispatchDomainEventsAsync(cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
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
                await DispatchDomainEventsAsync(cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
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

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken = default)
    {
        var entities = _context.ChangeTracker
            .Entries<BaseAuditableEntity>()
            .Where(x => x.Entity.DomainEvents != null && x.Entity.DomainEvents.Any());

        var domainEvents = entities
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList();

        entities.ToList()
            .ForEach(entity => entity.Entity.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
        {
            await _domainEventService.PublishAsync(domainEvent, cancellationToken);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _context.Dispose();
            _disposed = true;
        }
    }
}