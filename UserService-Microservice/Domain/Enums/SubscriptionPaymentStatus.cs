namespace UserService.Domain.Enums;

public enum SubscriptionPaymentStatus
{
    Pending = 0,
    Processing = 1,
    Paid = 2,
    Failed = 3,
    Cancelled = 4,
    Expired = 5
}
