using UserService.Domain.Common;

namespace UserService.Domain.Entities;

public class SubscriptionPlan : BaseAuditableEntity, ISoftDelete
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "VND";
    public int DurationDays { get; set; }
    public int QuotaLimit { get; set; }
    public List<string> Features { get; set; } = new();
    public bool IsActive { get; set; } = true;
    
    // Foreign key to SubscriptionTier
    public Guid SubscriptionTierId { get; set; }
    public virtual SubscriptionTier Tier { get; set; } = null!;

    // ISoftDelete implementation
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}
