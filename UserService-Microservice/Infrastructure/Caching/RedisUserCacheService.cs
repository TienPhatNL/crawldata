using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Text.Json;
using UserService.Domain.Common;
using UserService.Domain.Interfaces;

namespace UserService.Infrastructure.Caching;

/// <summary>
/// Redis cache service with stampede prevention using request coalescing and TTL jittering
/// </summary>
public class RedisUserCacheService : IUserCacheService
{
    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer _redis;
    private readonly RedisCacheSettings _settings;
    private readonly ILogger<RedisUserCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    
    // Stampede prevention: per-key locks for request coalescing
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    
    // Statistics
    private long _cacheHits = 0;
    private long _cacheMisses = 0;
    private long _stampedePrevented = 0;
    
    public RedisUserCacheService(
        IDistributedCache cache,
        IConnectionMultiplexer redis,
        IOptions<RedisCacheSettings> settings,
        ILogger<RedisUserCacheService> logger)
    {
        _cache = cache;
        _redis = redis;
        _settings = settings.Value;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        
        _logger.LogInformation("RedisUserCacheService initialized with prefix: {Prefix}, jitter: {Jitter}%", 
            _settings.KeyPrefix, _settings.TtlJitterPercent);
    }
    
    public async Task<TDto?> GetAsync<TDto>(string key, CancellationToken cancellationToken = default) where TDto : class
    {
        if (!_settings.Enabled) return null;
        
        try
        {
            var fullKey = GetFullKey(key);
            var json = await _cache.GetStringAsync(fullKey, cancellationToken);
            
            if (json != null)
            {
                Interlocked.Increment(ref _cacheHits);
                _logger.LogDebug("Cache HIT: {Key}", key);
                return JsonSerializer.Deserialize<TDto>(json, _jsonOptions);
            }
            
            Interlocked.Increment(ref _cacheMisses);
            _logger.LogDebug("Cache MISS: {Key}", key);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache key: {Key}", key);
            return null;
        }
    }
    
    public async Task SetAsync<TDto>(string key, TDto value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where TDto : class
    {
        if (!_settings.Enabled || value == null) return;
        
        try
        {
            var fullKey = GetFullKey(key);
            var ttl = expiration ?? GetJitteredTtl(TimeSpan.FromMinutes(_settings.DefaultExpirationMinutes));
            var json = JsonSerializer.Serialize(value, _jsonOptions);
            
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            };
            
            await _cache.SetStringAsync(fullKey, json, options, cancellationToken);
            _logger.LogDebug("Cache SET: {Key}, TTL: {Ttl}s", key, ttl.TotalSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache key: {Key}", key);
        }
    }
    
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled) return false;
        
        try
        {
            var fullKey = GetFullKey(key);
            var db = _redis.GetDatabase();
            return await db.KeyExistsAsync(fullKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache key existence: {Key}", key);
            return false;
        }
    }
    
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled) return;
        
        try
        {
            var fullKey = GetFullKey(key);
            await _cache.RemoveAsync(fullKey, cancellationToken);
            _logger.LogDebug("Cache REMOVE: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache key: {Key}", key);
        }
    }
    
    public async Task<Dictionary<string, TDto?>> GetManyAsync<TDto>(IEnumerable<string> keys, CancellationToken cancellationToken = default) where TDto : class
    {
        var result = new Dictionary<string, TDto?>();
        
        if (!_settings.Enabled) return result;
        
        foreach (var key in keys)
        {
            var value = await GetAsync<TDto>(key, cancellationToken);
            result[key] = value;
        }
        
        return result;
    }
    
    public async Task SetManyAsync<TDto>(Dictionary<string, TDto> items, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where TDto : class
    {
        if (!_settings.Enabled || items == null || !items.Any()) return;
        
        var tasks = items.Select(kvp => SetAsync(kvp.Key, kvp.Value, expiration, cancellationToken));
        await Task.WhenAll(tasks);
    }
    
    public async Task RemoveManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled || keys == null || !keys.Any()) return;
        
        var tasks = keys.Select(key => RemoveAsync(key, cancellationToken));
        await Task.WhenAll(tasks);
    }
    
    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled) return;
        
        try
        {
            var fullPattern = GetFullKey(pattern);
            var db = _redis.GetDatabase();
            var endpoints = _redis.GetEndPoints();
            
            foreach (var endpoint in endpoints)
            {
                var server = _redis.GetServer(endpoint);
                var keys = server.Keys(pattern: fullPattern).ToArray();
                
                if (keys.Any())
                {
                    await db.KeyDeleteAsync(keys);
                    _logger.LogInformation("Cache REMOVE by pattern: {Pattern}, removed {Count} keys", pattern, keys.Length);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache keys by pattern: {Pattern}", pattern);
        }
    }
    
    /// <summary>
    /// STAMPEDE PREVENTION: Get from cache or fetch with request coalescing
    /// </summary>
    public async Task<TDto?> GetOrFetchAsync<TDto>(
        string key,
        Func<Task<TDto?>> fetchFunc,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where TDto : class
    {
        if (!_settings.Enabled)
        {
            return await fetchFunc();
        }
        
        // Try cache first (fast path)
        var cached = await GetAsync<TDto>(key, cancellationToken);
        if (cached != null)
        {
            return cached;
        }
        
        // Cache miss - use stampede prevention
        var lockKey = $"lock:{key}";
        var semaphore = _locks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));
        
        var waitedForLock = false;
        try
        {
            // Try to acquire lock
            var acquired = await semaphore.WaitAsync(0, cancellationToken);
            
            if (!acquired)
            {
                // Another request is fetching, wait for it
                waitedForLock = true;
                Interlocked.Increment(ref _stampedePrevented);
                _logger.LogDebug("Stampede prevented: waiting for lock on {Key}", key);
                
                await semaphore.WaitAsync(cancellationToken);
            }
            
            // Double-check cache (another request might have filled it)
            cached = await GetAsync<TDto>(key, cancellationToken);
            if (cached != null)
            {
                if (waitedForLock)
                {
                    _logger.LogDebug("Stampede prevented: cache filled by another request for {Key}", key);
                }
                return cached;
            }
            
            // Fetch from source (only this request does it)
            _logger.LogDebug("Fetching from source: {Key}", key);
            var fetched = await fetchFunc();
            
            if (fetched != null)
            {
                // Cache with jittered TTL
                await SetAsync(key, fetched, expiration, cancellationToken);
            }
            
            return fetched;
        }
        finally
        {
            semaphore.Release();
            
            // Clean up lock after delay to avoid dictionary bloat
            _ = Task.Delay(TimeSpan.FromMinutes(_settings.LockCleanupDelayMinutes), cancellationToken)
                .ContinueWith(_ => _locks.TryRemove(lockKey, out var _), TaskScheduler.Default);
        }
    }
    
    // User-specific helper methods
    
    public Task<TDto?> GetUserByIdAsync<TDto>(Guid userId, CancellationToken cancellationToken = default) where TDto : class
    {
        return GetAsync<TDto>($"user:{userId}", cancellationToken);
    }
    
    public Task SetUserAsync<TDto>(Guid userId, TDto user, CancellationToken cancellationToken = default) where TDto : class
    {
        var expiration = GetJitteredTtl(TimeSpan.FromMinutes(_settings.UserInfoExpirationMinutes));
        return SetAsync($"user:{userId}", user, expiration, cancellationToken);
    }
    
    public Task<TDto?> GetOrFetchUserAsync<TDto>(
        Guid userId,
        Func<Task<TDto?>> fetchFunc,
        CancellationToken cancellationToken = default) where TDto : class
    {
        var expiration = GetJitteredTtl(TimeSpan.FromMinutes(_settings.UserInfoExpirationMinutes));
        return GetOrFetchAsync($"user:{userId}", fetchFunc, expiration, cancellationToken);
    }
    
    public Task InvalidateUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return RemoveAsync($"user:{userId}", cancellationToken);
    }
    
    public async Task<bool?> GetUserValidationAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var key = $"validation:{userId}";
        var cached = await GetAsync<CacheWrapper<bool>>(key, cancellationToken);
        return cached?.Value;
    }
    
    public Task SetUserValidationAsync(Guid userId, bool isValid, CancellationToken cancellationToken = default)
    {
        var key = $"validation:{userId}";
        var expiration = GetJitteredTtl(TimeSpan.FromMinutes(_settings.ValidationExpirationMinutes));
        return SetAsync(key, new CacheWrapper<bool> { Value = isValid }, expiration, cancellationToken);
    }
    
    // Helper methods
    
    private string GetFullKey(string key)
    {
        return $"{_settings.KeyPrefix}:{key}";
    }
    
    private TimeSpan GetJitteredTtl(TimeSpan baseTtl)
    {
        if (_settings.TtlJitterPercent <= 0)
        {
            return baseTtl;
        }
        
        var jitterPercent = Random.Shared.Next(-_settings.TtlJitterPercent, _settings.TtlJitterPercent + 1) / 100.0;
        var jitterSeconds = baseTtl.TotalSeconds * jitterPercent;
        var jitteredTtl = TimeSpan.FromSeconds(baseTtl.TotalSeconds + jitterSeconds);
        
        return jitteredTtl;
    }
    
    // Statistics logging (called periodically or on demand)
    public void LogStatistics()
    {
        if (!_settings.EnableStatistics) return;
        
        var total = _cacheHits + _cacheMisses;
        var hitRatio = total > 0 ? (_cacheHits * 100.0 / total) : 0;
        
        _logger.LogInformation(
            "Cache Statistics - Hits: {Hits}, Misses: {Misses}, Hit Ratio: {HitRatio:F2}%, Stampede Prevented: {Stampede}",
            _cacheHits, _cacheMisses, hitRatio, _stampedePrevented);
    }
    
    // Wrapper class for primitive types
    private class CacheWrapper<T>
    {
        public T Value { get; set; } = default!;
    }
}
