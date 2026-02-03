namespace UserService.Application.Features.SubscriptionPlans.DTOs;

public class CreateSubscriptionPlanDto
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "VND";
    public int DurationDays { get; set; }
    public int QuotaLimit { get; set; }
    public List<string> Features { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public Guid SubscriptionTierId { get; set; }
}
