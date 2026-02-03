namespace UserService.Domain.Events;

/// <summary>
/// Event published when user cache needs to be invalidated
/// </summary>
public class UserCacheInvalidationEvent
{
    /// <summary>
    /// User ID to invalidate
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Type of invalidation event
    /// </summary>
    public InvalidationType Type { get; set; }
    
    /// <summary>
    /// Timestamp when the event occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Optional reason for invalidation
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Types of cache invalidation events
/// </summary>
public enum InvalidationType
{
    /// <summary>
    /// User information was updated
    /// </summary>
    UserUpdated,
    
    /// <summary>
    /// User was deleted
    /// </summary>
    UserDeleted,
    
    /// <summary>
    /// User role was changed
    /// </summary>
    RoleChanged,
    
    /// <summary>
    /// User status was changed
    /// </summary>
    StatusChanged,
    
    /// <summary>
    /// User profile was updated
    /// </summary>
    ProfileUpdated
}
