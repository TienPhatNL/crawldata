namespace NotificationService.Domain.Interfaces;

public interface INotificationCacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task InvalidateUserNotificationsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task InvalidateUserPreferencesAsync(Guid userId, CancellationToken cancellationToken = default);
}
