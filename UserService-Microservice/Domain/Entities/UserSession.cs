using UserService.Domain.Common;

namespace UserService.Domain.Entities;

public class UserSession : BaseEntity
{
    public Guid UserId { get; set; }
    public string SessionToken { get; set; } = null!;
    public string? RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? DeviceInfo { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastActivityAt { get; set; }
    public DateTime? LoggedOutAt { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    
    // Computed properties
    public bool IsExpired => ExpiresAt < DateTime.UtcNow;
    public bool IsRefreshTokenExpired => RefreshTokenExpiresAt.HasValue && RefreshTokenExpiresAt.Value < DateTime.UtcNow;
    public bool IsValidSession => IsActive && !IsExpired;
}