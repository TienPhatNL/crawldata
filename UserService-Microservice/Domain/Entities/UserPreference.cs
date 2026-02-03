using UserService.Domain.Common;

namespace UserService.Domain.Entities;

public class UserPreference : BaseAuditableEntity
{
    public Guid UserId { get; set; }
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsSystem { get; set; } = false; // System preferences vs user preferences
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
}