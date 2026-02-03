using UserService.Domain.Common;

namespace UserService.Domain.Events;

public class UserSubscriptionUpgradedEvent : BaseEvent
{
    public Guid UserId { get; }
    public string UserEmail { get; }
    public Entities.SubscriptionTier? OldTier { get; }
    public Entities.SubscriptionTier? NewTier { get; }
    public int NewQuotaLimit { get; }

    public UserSubscriptionUpgradedEvent(Guid userId, string userEmail, Entities.SubscriptionTier? oldTier, Entities.SubscriptionTier? newTier, int newQuotaLimit)
    {
        UserId = userId;
        UserEmail = userEmail;
        OldTier = oldTier;
        NewTier = newTier;
        NewQuotaLimit = newQuotaLimit;
    }
}