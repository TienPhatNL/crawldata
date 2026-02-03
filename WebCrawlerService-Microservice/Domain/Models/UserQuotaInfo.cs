namespace WebCrawlerService.Domain.Models;

public class UserQuotaInfo
{
    public Guid UserId { get; set; }
    public int TotalQuota { get; set; }
    public int RemainingQuota { get; set; }
    public string PlanType { get; set; } = string.Empty;
    public DateTime? ResetDate { get; set; }
    public DateTime LastUpdated { get; set; }
}
