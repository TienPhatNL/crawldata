using UserService.Domain.Enums;

namespace UserService.Application.Features.Admin.Dashboard.DTOs;

public class UserStatisticsDto
{
    public int TotalUsers { get; set; }
    public Dictionary<string, int> UsersByTier { get; set; } = new();
    public Dictionary<UserRole, int> UsersByRole { get; set; } = new();
    public int NewUsers { get; set; }
    public int ActiveUsers { get; set; }
    public decimal ConversionRate { get; set; } // Free to Paid conversion
    public decimal AverageLifetimeValue { get; set; }
    public List<UserGrowthTimelineItem> Timeline { get; set; } = new();
    public List<UserNearQuotaLimit> UsersNearQuota { get; set; } = new();
}

public class UserGrowthTimelineItem
{
    public DateTime Date { get; set; }
    public int NewUsers { get; set; }
    public int TotalUsers { get; set; }
    public int PaidUsers { get; set; }
}

public class UserNearQuotaLimit
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string CurrentTier { get; set; } = null!;
    public int QuotaUsed { get; set; }
    public int QuotaLimit { get; set; }
    public decimal UsagePercentage { get; set; }
}
