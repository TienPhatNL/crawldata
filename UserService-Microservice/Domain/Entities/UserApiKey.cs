using UserService.Domain.Common;
using UserService.Domain.Enums;

namespace UserService.Domain.Entities;

public class UserApiKey : BaseAuditableEntity, ISoftDelete
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public string KeyHash { get; set; } = null!;
    public string KeyPrefix { get; set; } = null!; // First 8 chars for display
    public DateTime? LastUsedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Scopes { get; set; } // Comma-separated scopes
    public string? Description { get; set; }
    public string? IpWhitelist { get; set; } // Comma-separated IP addresses
    public int UsageCount { get; set; } = 0;
    public int? RateLimitPerHour { get; set; }
    
    // Soft delete support
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    
    // Computed properties
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;
    public bool IsValid => IsActive && !IsExpired && !IsDeleted;
}