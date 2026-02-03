using UserService.Domain.Common;
using UserService.Domain.Enums;

namespace UserService.Domain.Entities;

public class UserSubscription : BaseAuditableEntity
{
    public Guid? UserId { get; set; }
    public Guid SubscriptionPlanId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public bool AutoRenew { get; set; } = false;
    public DateTime? LastBillingDate { get; set; }
    public DateTime? NextBillingDate { get; set; }
    public decimal? Price { get; set; }
    public string Currency { get; set; } = "USD";
    public string? ExternalSubscriptionId { get; set; } // For payment provider integration
    public string? PaymentReference { get; set; }
    
    // Cancellation support
    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    
    // Features enabled by this subscription
    public int QuotaLimit { get; set; }
    public int DataExtractionLimit { get; set; }
    public int ReportGenerationLimit { get; set; }
    public bool AdvancedAnalyticsEnabled { get; set; } = false;
    public bool PrioritySupport { get; set; } = false;
    public bool ApiAccessEnabled { get; set; } = false;
    
    // Navigation properties
    public virtual User? User { get; set; }
    public virtual SubscriptionPlan SubscriptionPlan { get; set; } = null!;
    
    // Computed properties
    public bool IsExpired => EndDate.HasValue && EndDate.Value < DateTime.UtcNow;
    public bool IsValidSubscription => IsActive && !IsExpired;
}