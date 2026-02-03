using UserService.Domain.Common;
using UserService.Domain.Enums;

namespace UserService.Domain.Entities;

public class UserQuotaSnapshot : BaseAuditableEntity
{
    public Guid UserId { get; set; }
    public Guid? SubscriptionPlanId { get; set; }
    public int QuotaLimit { get; set; }
    public int QuotaUsed { get; set; }
    public DateTime QuotaResetDate { get; set; }
    public DateTime LastSynchronizedAt { get; set; }
    public string Source { get; set; } = "subscription";
    public bool IsOverride { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual SubscriptionPlan? SubscriptionPlan { get; set; }
}
