namespace UserService.Application.Features.Admin.Dashboard.DTOs;

public class SubscriptionStatisticsDto
{
    public int TotalActiveSubscriptions { get; set; }
    public Dictionary<string, int> SubscriptionsByTier { get; set; } = new();
    public int NewSubscriptions { get; set; }
    public decimal ChurnRate { get; set; }
    public decimal RenewalRate { get; set; }
    public UpgradeDowngradeStats UpgradeDowngrade { get; set; } = new();
    public List<SubscriptionTimelineItem> Timeline { get; set; } = new();
    public decimal AverageSubscriptionValue { get; set; }
}

public class UpgradeDowngradeStats
{
    public int Upgrades { get; set; }
    public int Downgrades { get; set; }
    public int NetChange { get; set; }
}

public class SubscriptionTimelineItem
{
    public DateTime Date { get; set; }
    public int NewSubscriptions { get; set; }
    public int CancelledSubscriptions { get; set; }
    public int ActiveTotal { get; set; }
}
