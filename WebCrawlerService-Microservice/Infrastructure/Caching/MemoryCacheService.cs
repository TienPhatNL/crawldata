using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using WebCrawlerService.Domain.Common;
using WebCrawlerService.Domain.Interfaces;

namespace WebCrawlerService.Infrastructure.Caching;

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly CacheSettings _cacheSettings;
    private readonly ConcurrentDictionary<string, bool> _cacheKeys;

    public MemoryCacheService(IMemoryCache memoryCache, IOptions<CacheSettings> cacheSettings)
    {
        _memoryCache = memoryCache;
        _cacheSettings = cacheSettings.Value;
        _cacheKeys = new ConcurrentDictionary<string, bool>();
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        var result = _memoryCache.Get<T>(key);
        return Task.FromResult(result);
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItem, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
    {
        if (_memoryCache.TryGetValue(key, out T? cachedValue) && cachedValue != null)
        {
            return cachedValue;
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
            options.SetAbsoluteExpiration(_cacheSettings.DefaultExpiry);
        }

        _memoryCache.Set(key, value, options);
        _cacheKeys.TryAdd(key, true);
        
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _memoryCache.Remove(key);
        _cacheKeys.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        var regex = new Regex(pattern.Replace("*", ".*"));
        var keysToRemove = _cacheKeys.Keys.Where(key => regex.IsMatch(key)).ToList();
        
        foreach (var key in keysToRemove)
        {
            _memoryCache.Remove(key);
            _cacheKeys.TryRemove(key, out _);
        }
        
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var exists = _memoryCache.TryGetValue(key, out _);
        return Task.FromResult(exists);
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        if (_memoryCache is MemoryCache concreteCache)
        {
            concreteCache.Compact(1.0);
        }
        
        _cacheKeys.Clear();
        return Task.CompletedTask;
    }

    public Task RefreshAsync(string key, CancellationToken cancellationToken = default)
    {
        // Memory cache doesn't need explicit refresh
        return Task.CompletedTask;
    }
}