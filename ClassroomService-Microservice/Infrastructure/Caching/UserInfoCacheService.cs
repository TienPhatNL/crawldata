using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Text.Json;
using ClassroomService.Domain.Common;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Infrastructure.Caching;

/// <summary>
/// User info cache service for ClassroomService with stampede prevention
/// </summary>
public class UserInfoCacheService : IUserInfoCacheService
{
    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer _redis;
    private readonly RedisCacheSettings _settings;
    private readonly ILogger<UserInfoCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    
    // Stampede prevention: per-key locks for request coalescing
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    
    // Statistics
    private long _cacheHits = 0;
    private long _cacheMisses = 0;
    private long _stampedePrevented = 0;
    
    public UserInfoCacheService(
        IDistributedCache cache,
        IConnectionMultiplexer redis,
        IOptions<RedisCacheSettings> settings,
        ILogger<UserInfoCacheService> logger)
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
        
        _logger.LogInformation("UserInfoCacheService initialized with prefix: {Prefix}, jitter: {Jitter}%", 
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
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Try cache first (fast path)
        var cached = await GetAsync<TDto>(key, cancellationToken);
        if (cached != null)
        {
            Interlocked.Increment(ref _cacheHits);
            stopwatch.Stop();
            _logger.LogInformation("⚡ [CACHE HIT] {Key} | {Time}ms | Hit Rate: {HitRate:F1}%", 
                key, stopwatch.ElapsedMilliseconds, GetHitRatePercentage());
            return cached;
        }
        
        // Cache miss
        Interlocked.Increment(ref _cacheMisses);
        _logger.LogInformation("🔍 [CACHE MISS] {Key} | Will fetch via Kafka", key);
        
        // Use stampede prevention
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
                _logger.LogInformation("🔒 [STAMPEDE PREVENTED] {Key} | Waiting for concurrent fetch...", key);
                
                await semaphore.WaitAsync(cancellationToken);
            }
            
            // Double-check cache (another request might have filled it)
            cached = await GetAsync<TDto>(key, cancellationToken);
            if (cached != null)
            {
                if (waitedForLock)
                {
                    stopwatch.Stop();
                    _logger.LogInformation("✅ [STAMPEDE RESOLVED] {Key} | Filled by concurrent request | Wait: {Time}ms", 
                        key, stopwatch.ElapsedMilliseconds);
                }
                return cached;
            }
            
            // Fetch from source (only this request does it)
            _logger.LogInformation("⏳ [FETCHING] {Key} | Querying UserService via Kafka...", key);
            var fetchStart = System.Diagnostics.Stopwatch.StartNew();
            var fetched = await fetchFunc();
            fetchStart.Stop();
            
            if (fetched != null)
            {
                // Cache with jittered TTL
                await SetAsync(key, fetched, expiration, cancellationToken);
                stopwatch.Stop();
                var ttlMinutes = expiration?.TotalMinutes ?? 0;
                _logger.LogInformation("✅ [CACHED] {Key} | Fetch: {FetchTime}ms | Total: {TotalTime}ms | TTL: {TTL:F0}min", 
                    key, fetchStart.ElapsedMilliseconds, stopwatch.ElapsedMilliseconds, ttlMinutes);
            }
            else
            {
                stopwatch.Stop();
                _logger.LogWarning("❌ [FETCH FAILED] {Key} | Source returned null | Time: {Time}ms", 
                    key, stopwatch.ElapsedMilliseconds);
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
    
    public Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Use userservice prefix to read from UserService's cache
        return GetAsync<UserDto>($"userservice:user:{userId}", cancellationToken);
    }
    
    public Task SetUserAsync(Guid userId, UserDto user, CancellationToken cancellationToken = default)
    {
        var expiration = GetJitteredTtl(TimeSpan.FromMinutes(_settings.UserInfoExpirationMinutes));
        // Use userservice prefix to write to UserService's cache namespace
        return SetAsync($"userservice:user:{userId}", user, expiration, cancellationToken);
    }
    
    public Task<UserDto?> GetOrFetchUserAsync(
        Guid userId,
        Func<Task<UserDto?>> fetchFunc,
        CancellationToken cancellationToken = default)
    {
        var expiration = GetJitteredTtl(TimeSpan.FromMinutes(_settings.UserInfoExpirationMinutes));
        // Use userservice prefix to share cache with UserService
        return GetOrFetchAsync($"userservice:user:{userId}", fetchFunc, expiration, cancellationToken);
    }
    
    public Task InvalidateUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Use userservice prefix to invalidate from shared cache
        return RemoveAsync($"userservice:user:{userId}", cancellationToken);
    }
    
    public async Task<Dictionary<Guid, UserDto?>> GetUsersByIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<Guid, UserDto?>();
        
        foreach (var userId in userIds)
        {
            var user = await GetUserByIdAsync(userId, cancellationToken);
            result[userId] = user;
        }
        
        return result;
    }
    
    public async Task SetUsersAsync(Dictionary<Guid, UserDto> users, CancellationToken cancellationToken = default)
    {
        var tasks = users.Select(kvp => SetUserAsync(kvp.Key, kvp.Value, cancellationToken));
        await Task.WhenAll(tasks);
    }
    
    // Helper methods
    
    private string GetFullKey(string key)
    {
        // If key already has a prefix (like userservice:user:guid), don't add classroom prefix
        if (key.Contains(':'))
        {
            return key;
        }
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
    
    private double GetHitRatePercentage()
    {
        var totalRequests = _cacheHits + _cacheMisses;
        return totalRequests == 0 ? 0 : (_cacheHits * 100.0 / totalRequests);
    }
    
    // Statistics logging
    public void LogStatistics()
    {
        if (!_settings.EnableStatistics) return;
        
        var total = _cacheHits + _cacheMisses;
        var hitRatio = total > 0 ? (_cacheHits * 100.0 / total) : 0;
        
        _logger.LogInformation(
            "Cache Statistics - Hits: {Hits}, Misses: {Misses}, Hit Ratio: {HitRatio:F2}%, Stampede Prevented: {Stampede}",
            _cacheHits, _cacheMisses, hitRatio, _stampedePrevented);
    }
    
    // ==================== Assignment Context Caching ====================
    
    public async Task<AssignmentContextDto?> GetOrFetchAssignmentContextAsync(
        Guid assignmentId, 
        CancellationToken cancellationToken = default)
    {
        var key = $"assignment:context:{assignmentId}";
        
        // Try get from cache first
        var cached = await GetAsync<AssignmentContextDto>(key, cancellationToken);
        if (cached != null)
        {
            _logger.LogDebug("Assignment context cache HIT: {AssignmentId}", assignmentId);
            return cached;
        }
        
        _logger.LogDebug("Assignment context cache MISS: {AssignmentId}", assignmentId);
        return null; // Caller should fetch from DB and cache it
    }
    
    public async Task SetAssignmentContextAsync(
        Guid assignmentId, 
        AssignmentContextDto assignment, 
        CancellationToken cancellationToken = default)
    {
        var key = $"assignment:context:{assignmentId}";
        
        // Cache for 60 minutes (assignments don't change frequently)
        var expiration = TimeSpan.FromMinutes(60);
        
        await SetAsync(key, assignment, expiration, cancellationToken);
        _logger.LogInformation("Cached assignment context: {AssignmentId}, Title: {Title}", 
            assignmentId, assignment.Title);
    }
    
    public async Task InvalidateAssignmentAsync(Guid assignmentId, CancellationToken cancellationToken = default)
    {
        var key = $"assignment:context:{assignmentId}";
        await RemoveAsync(key, cancellationToken);
        _logger.LogInformation("Invalidated assignment cache: {AssignmentId}", assignmentId);
    }
}
