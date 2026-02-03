namespace UserService.Application.Features.SubscriptionPlans.DTOs;

public class SubscriptionPlanDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "VND";
    public int DurationDays { get; set; }
    public int QuotaLimit { get; set; }
    public List<string> Features { get; set; } = new();
    public bool IsActive { get; set; }
    public Guid SubscriptionTierId { get; set; }
    public string? TierName { get; set; }
    public int? TierLevel { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
