using UserService.Domain.Common;
using UserService.Domain.Enums;

namespace UserService.Domain.Entities;

public class SubscriptionPayment : BaseAuditableEntity
{
    public Guid UserId { get; set; }
    public Guid SubscriptionPlanId { get; set; }
    public SubscriptionPaymentStatus Status { get; set; } = SubscriptionPaymentStatus.Pending;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public string? PaymentLinkId { get; set; }
    public string? OrderCode { get; set; }
    public string? CheckoutUrl { get; set; }
    public DateTime? ExpiredAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? FailureReason { get; set; }
    public string? PaymentReference { get; set; }
    public string? PayOSPayload { get; set; }
    public string? Signature { get; set; }
    public virtual User? User { get; set; }
    public virtual SubscriptionPlan? SubscriptionPlan { get; set; }
}
