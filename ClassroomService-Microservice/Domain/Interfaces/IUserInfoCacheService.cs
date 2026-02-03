using ClassroomService.Domain.DTOs;

namespace ClassroomService.Domain.Interfaces;

/// <summary>
/// Service for accessing cached user information from UserService with stampede prevention
/// </summary>
public interface IUserInfoCacheService
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
    
    // Stampede prevention method
    
    /// <summary>
    /// Get from cache or fetch using the provided function with stampede prevention
    /// </summary>
    Task<TDto?> GetOrFetchAsync<TDto>(
        string key,
        Func<Task<TDto?>> fetchFunc,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where TDto : class;
    
    // User-specific helper methods
    
    /// <summary>
    /// Get user by ID from cache
    /// </summary>
    Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cache user information
    /// </summary>
    Task SetUserAsync(Guid userId, UserDto user, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get user from cache or fetch with stampede prevention
    /// </summary>
    Task<UserDto?> GetOrFetchUserAsync(
        Guid userId,
        Func<Task<UserDto?>> fetchFunc,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Invalidate (remove) user from cache
    /// </summary>
    Task InvalidateUserAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get multiple users by IDs from cache
    /// </summary>
    Task<Dictionary<Guid, UserDto?>> GetUsersByIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cache multiple users
    /// </summary>
    Task SetUsersAsync(Dictionary<Guid, UserDto> users, CancellationToken cancellationToken = default);
    
    // Assignment-specific helper methods
    
    /// <summary>
    /// Get assignment context from cache or fetch from DB
    /// </summary>
    Task<AssignmentContextDto?> GetOrFetchAssignmentContextAsync(
        Guid assignmentId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Set assignment context in cache
    /// </summary>
    Task SetAssignmentContextAsync(
        Guid assignmentId, 
        AssignmentContextDto assignment, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Invalidate assignment cache when assignment is updated
    /// </summary>
    Task InvalidateAssignmentAsync(Guid assignmentId, CancellationToken cancellationToken = default);
}
