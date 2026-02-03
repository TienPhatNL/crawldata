using UserService.Domain.Enums;

namespace UserService.Application.Features.Admin.Dashboard.DTOs;

public class PaymentStatisticsDto
{
    public int TotalOrders { get; set; }
    public int NewOrders { get; set; }
    public Dictionary<SubscriptionPaymentStatus, int> StatusDistribution { get; set; } = new();
    public decimal SuccessRate { get; set; }
    public List<FailedPaymentInfo> FailedPayments { get; set; } = new();
    public List<PaymentTimelineItem> Timeline { get; set; } = new();
    public decimal AverageProcessingTime { get; set; } // in seconds
}

public class FailedPaymentInfo
{
    public string Reason { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
}

public class PaymentTimelineItem
{
    public DateTime Date { get; set; }
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
}
