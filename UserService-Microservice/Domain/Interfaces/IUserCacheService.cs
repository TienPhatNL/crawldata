namespace UserService.Domain.Interfaces;

/// <summary>
/// Service for caching user information in Redis with LRU eviction and stampede prevention
/// </summary>
public interface IUserCacheService
{
    /// <summary>
    /// Get a cached value by key
    /// </summary>
    Task<TDto?> GetAsync<TDto>(string key, CancellationToken cancellationToken = default) where TDto : class;
    
    /// <summary>
    /// Set a value in cache with optional expiration
    /// </summary>
    Task SetAsync<TDto>(string key, TDto value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where TDto : class;
    
    /// <summary>
    /// Check if a key exists in cache
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Remove a single key from cache
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get multiple cached values by keys (batch operation)
    /// </summary>
    Task<Dictionary<string, TDto?>> GetManyAsync<TDto>(IEnumerable<string> keys, CancellationToken cancellationToken = default) where TDto : class;
    
    /// <summary>
    /// Set multiple values in cache (batch operation)
    /// </summary>
    Task SetManyAsync<TDto>(Dictionary<string, TDto> items, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where TDto : class;
    
    /// <summary>
    /// Remove multiple keys from cache (batch operation)
    /// </summary>
    Task RemoveManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Remove all keys matching a pattern (e.g., "user:*")
    /// </summary>
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);
    
    // Stampede prevention method
    
    /// <summary>
    /// Get from cache or fetch using the provided function with stampede prevention.
    /// Uses request coalescing to ensure only one concurrent request fetches on cache miss.
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="fetchFunc">Function to fetch data on cache miss</param>
    /// <param name="expiration">Optional expiration time (uses default with jitter if null)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<TDto?> GetOrFetchAsync<TDto>(
        string key,
        Func<Task<TDto?>> fetchFunc,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where TDto : class;
    
    // User-specific helper methods
    
    /// <summary>
    /// Get user by ID from cache
    /// </summary>
    Task<TDto?> GetUserByIdAsync<TDto>(Guid userId, CancellationToken cancellationToken = default) where TDto : class;
    
    /// <summary>
    /// Cache user information with default user expiration and jitter
    /// </summary>
    Task SetUserAsync<TDto>(Guid userId, TDto user, CancellationToken cancellationToken = default) where TDto : class;
    
    /// <summary>
    /// Get user from cache or fetch with stampede prevention
    /// </summary>
    Task<TDto?> GetOrFetchUserAsync<TDto>(
        Guid userId,
        Func<Task<TDto?>> fetchFunc,
        CancellationToken cancellationToken = default) where TDto : class;
    
    /// <summary>
    /// Invalidate (remove) user from cache
    /// </summary>
    Task InvalidateUserAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get user validation result from cache
    /// </summary>
    Task<bool?> GetUserValidationAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cache user validation result with shorter TTL
    /// </summary>
    Task SetUserValidationAsync(Guid userId, bool isValid, CancellationToken cancellationToken = default);
}
