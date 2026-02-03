namespace UserService.Domain.Common;

/// <summary>
/// Configuration settings for Redis caching in UserService with stampede prevention
/// </summary>
public class RedisCacheSettings
{
    public const string SectionName = "RedisCache";
    
    /// <summary>
    /// Whether caching is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Default expiration time in minutes for cache entries
    /// </summary>
    public int DefaultExpirationMinutes { get; set; } = 60;
    
    /// <summary>
    /// User information cache expiration in minutes
    /// </summary>
    public int UserInfoExpirationMinutes { get; set; } = 30;
    
    /// <summary>
    /// User validation cache expiration in minutes
    /// </summary>
    public int ValidationExpirationMinutes { get; set; } = 15;
    
    /// <summary>
    /// Prefix for all cache keys to avoid collisions
    /// </summary>
    public string KeyPrefix { get; set; } = "userservice";
    
    /// <summary>
    /// Redis eviction policy (e.g., allkeys-lru, volatile-lru)
    /// </summary>
    public string EvictionPolicy { get; set; } = "allkeys-lru";
    
    /// <summary>
    /// Maximum memory for Redis in MB
    /// </summary>
    public int MaxMemoryMb { get; set; } = 256;
    
    /// <summary>
    /// TTL jitter percentage to prevent synchronized expiration (0-100)
    /// Example: 10 means ±10% variance in TTL
    /// </summary>
    public int TtlJitterPercent { get; set; } = 10;
    
    /// <summary>
    /// Whether to enable cache statistics logging
    /// </summary>
    public bool EnableStatistics { get; set; } = true;
    
    /// <summary>
    /// Lock cleanup delay in minutes (for stampede prevention locks)
    /// </summary>
    public int LockCleanupDelayMinutes { get; set; } = 1;
}
