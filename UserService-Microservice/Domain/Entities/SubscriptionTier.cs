using UserService.Domain.Common;

namespace UserService.Domain.Entities;

/// <summary>
/// Subscription tier entity - replaces hardcoded enum
/// Allows dynamic tier management by admins
/// </summary>
public class SubscriptionTier : BaseAuditableEntity, ISoftDelete
{
    /// <summary>
    /// Tier name (e.g., "Free", "Basic", "Premium", "Enterprise")
    /// </summary>
    public string Name { get; set; } = null!;
    
    /// <summary>
    /// Tier description
    /// </summary>
    public string Description { get; set; } = null!;
    
    /// <summary>
    /// Tier level for comparison (0 = Free, 1 = Basic, 2 = Premium, 3 = Enterprise)
    /// </summary>
    public int Level { get; set; }
    
    /// <summary>
    /// Whether this tier is active and available for selection
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    // Soft delete implementation
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    
    // Navigation properties
    public virtual ICollection<SubscriptionPlan> SubscriptionPlans { get; set; } = new List<SubscriptionPlan>();
}
