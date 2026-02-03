namespace ClassroomService.Domain.Common;

/// <summary>
/// Configuration settings for Redis caching in ClassroomService
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
    /// Prefix for all cache keys to avoid collisions
    /// </summary>
    public string KeyPrefix { get; set; } = "classroom";
    
    /// <summary>
    /// TTL jitter percentage to prevent synchronized expiration (0-100)
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
