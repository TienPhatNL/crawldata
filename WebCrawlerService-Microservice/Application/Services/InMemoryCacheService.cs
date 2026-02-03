using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using WebCrawlerService.Application.Common.Interfaces;

namespace WebCrawlerService.Application.Services;

/// <summary>
/// Simple in-memory cache service for testing
/// TODO: Replace with distributed Redis cache for production
/// </summary>
public class InMemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<InMemoryCacheService> _logger;

    public InMemoryCacheService(IMemoryCache cache, ILogger<InMemoryCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        _cache.TryGetValue<T>(key, out var value);
        return Task.FromResult(value);
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItem, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
    {
        if (_cache.TryGetValue<T>(key, out var cached))
        {
            return cached!;
        }

        var item = await getItem();
        await SetAsync(key, item, expiry, cancellationToken);
        return item;
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
    {
        var options = new MemoryCacheEntryOptions();
        if (expiry.HasValue)
        {
            options.SetAbsoluteExpiration(expiry.Value);
        }
        else
        {
            options.SetSlidingExpiration(TimeSpan.FromMinutes(30));
        }

        _cache.Set(key, value, options);
        _logger.LogDebug("Cached item with key: {Key}", key);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.Remove(key);
        _logger.LogDebug("Removed cache item with key: {Key}", key);
        return Task.CompletedTask;
    }

    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        // MemoryCache doesn't support pattern matching, so we'll just log it
        _logger.LogWarning("Pattern-based cache removal not supported in InMemoryCacheService: {Pattern}", pattern);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var exists = _cache.TryGetValue(key, out _);
        return Task.FromResult(exists);
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        // MemoryCache doesn't have a clear all method
        _logger.LogWarning("Clear all not supported in InMemoryCacheService");
        return Task.CompletedTask;
    }

    public Task RefreshAsync(string key, CancellationToken cancellationToken = default)
    {
        // Just check if it exists to refresh sliding expiration
        _cache.TryGetValue(key, out _);
        return Task.CompletedTask;
    }
}
