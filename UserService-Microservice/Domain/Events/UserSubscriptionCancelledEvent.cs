using UserService.Domain.Common;

namespace UserService.Domain.Events;

public class UserSubscriptionCancelledEvent : BaseEvent
{
    public Guid UserId { get; }
    public string UserEmail { get; }
    public Entities.SubscriptionTier? PreviousTier { get; }
    public string? CancellationReason { get; }

    public UserSubscriptionCancelledEvent(Guid userId, string userEmail, Entities.SubscriptionTier? previousTier, string? cancellationReason)
    {
        UserId = userId;
        UserEmail = userEmail;
        PreviousTier = previousTier;
        CancellationReason = cancellationReason;
    }
}