namespace UserService.Application.Features.Admin.Dashboard.DTOs;

public class RevenueStatisticsDto
{
    public decimal TotalRevenue { get; set; }
    public string Currency { get; set; } = "VND";
    public Dictionary<string, decimal> RevenueByTier { get; set; } = new();
    public List<RevenueTimelineItem> Timeline { get; set; } = new();
    public RevenueGrowthDto Growth { get; set; } = new();
    public decimal AverageRevenuePerUser { get; set; }
    public decimal AverageOrderValue { get; set; }
}

public class RevenueTimelineItem
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public int OrderCount { get; set; }
}

public class RevenueGrowthDto
{
    public decimal Percentage { get; set; }
    public string ComparedTo { get; set; } = string.Empty; // "previous_period", "previous_month", etc.
    public decimal PreviousPeriodRevenue { get; set; }
    public decimal CurrentPeriodRevenue { get; set; }
}
