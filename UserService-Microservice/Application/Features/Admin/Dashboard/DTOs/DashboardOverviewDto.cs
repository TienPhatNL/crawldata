namespace UserService.Application.Features.Admin.Dashboard.DTOs;

public class DashboardOverviewDto
{
    public PeriodInfo Period { get; set; } = new();
    public RevenueStatisticsDto Revenue { get; set; } = new();
    public PaymentStatisticsDto Payments { get; set; } = new();
    public SubscriptionStatisticsDto Subscriptions { get; set; } = new();
    public UserStatisticsDto Users { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

public class PeriodInfo
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Description { get; set; } = string.Empty;
}
