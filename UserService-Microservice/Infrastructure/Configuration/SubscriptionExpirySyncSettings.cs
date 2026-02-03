namespace UserService.Infrastructure.Configuration;

public class SubscriptionExpirySyncSettings
{
    public const string SectionName = "SubscriptionExpirySync";

    public int IntervalSeconds { get; set; } = 3600; // Default: 1 hour
    public int BatchSize { get; set; } = 100;
}
