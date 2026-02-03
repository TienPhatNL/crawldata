using Microsoft.Extensions.Caching.Distributed;
using NotificationService.Domain.Interfaces;
using System.Text.Json;

namespace NotificationService.Infrastructure.Caching;

public class NotificationCacheService : INotificationCacheService
{
    private readonly IDistributedCache _cache;
    private const int DefaultExpirationMinutes = 30;

    public NotificationCacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        var cachedData = await _cache.GetStringAsync(key, cancellationToken);
        
        if (string.IsNullOrEmpty(cachedData))
        {
            return null;
        }

        return JsonSerializer.Deserialize<T>(cachedData);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(DefaultExpirationMinutes)
        };

        var serializedData = JsonSerializer.Serialize(value);
        await _cache.SetStringAsync(key, serializedData, options, cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(key, cancellationToken);
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var cachedData = await GetAsync<T>(key, cancellationToken);
        
        if (cachedData != null)
        {
            return cachedData;
        }

        var data = await factory();
        await SetAsync(key, data, expiration, cancellationToken);
        
        return data;
    }

    public async Task InvalidateUserNotificationsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await RemoveAsync($"user:{userId}:notifications", cancellationToken);
        await RemoveAsync($"user:{userId}:unread-count", cancellationToken);
    }

    public async Task InvalidateUserPreferencesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await RemoveAsync($"user:{userId}:preferences", cancellationToken);
    }
}
