using WebCrawlerService.Domain.Interfaces;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using WebCrawlerService.Domain.Common;

namespace WebCrawlerService.Infrastructure.Caching;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly CacheSettings _cacheSettings;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(
        IDistributedCache distributedCache,
        IConnectionMultiplexer connectionMultiplexer,
        IOptions<CacheSettings> cacheSettings)
    {
        _distributedCache = distributedCache;
        _connectionMultiplexer = connectionMultiplexer;
        _cacheSettings = cacheSettings.Value;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        var cachedValue = await _distributedCache.GetStringAsync(key, cancellationToken);
        
        if (cachedValue == null)
            return null;

        return JsonSerializer.Deserialize<T>(cachedValue, _jsonOptions);
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItem, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
    {
        var cachedValue = await GetAsync<T>(key, cancellationToken);
        
        if (cachedValue != null)
            return cachedValue;

        var item = await getItem();
        await SetAsync(key, item, expiry, cancellationToken);
        return item;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
    {
        var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
        
        var options = new DistributedCacheEntryOptions();
        if (expiry.HasValue)
        {
            options.SetAbsoluteExpiration(expiry.Value);
        }
        else
        {
            options.SetAbsoluteExpiration(_cacheSettings.DefaultExpiry);
        }

        await _distributedCache.SetStringAsync(key, serializedValue, options, cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _distributedCache.RemoveAsync(key, cancellationToken);
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        var database = _connectionMultiplexer.GetDatabase();
        var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
        
        var keys = server.Keys(pattern: pattern);
        foreach (var key in keys)
        {
            await database.KeyDeleteAsync(key);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var database = _connectionMultiplexer.GetDatabase();
        return await database.KeyExistsAsync(key);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
        await server.FlushDatabaseAsync();
    }

    public async Task RefreshAsync(string key, CancellationToken cancellationToken = default)
    {
        await _distributedCache.RefreshAsync(key, cancellationToken);
    }
}