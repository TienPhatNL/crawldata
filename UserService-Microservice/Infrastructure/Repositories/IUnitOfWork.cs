using UserService.Domain.Entities;

namespace UserService.Infrastructure.Repositories;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IRepository<UserApiKey> UserApiKeys { get; }
    IRepository<UserSubscription> UserSubscriptions { get; }
    IRepository<UserUsageRecord> UserUsageRecords { get; }
    IRepository<UserPreference> UserPreferences { get; }
    IRepository<UserSession> UserSessions { get; }
    IRepository<AllowedEmailDomain> AllowedEmailDomains { get; }
    IRepository<UserQuotaSnapshot> UserQuotaSnapshots { get; }
    IRepository<SubscriptionPayment> SubscriptionPayments { get; }
    IRepository<SubscriptionPlan> SubscriptionPlans { get; }
    IRepository<Announcement> Announcements { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task ExecuteTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default);
    Task<T> ExecuteTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);
}