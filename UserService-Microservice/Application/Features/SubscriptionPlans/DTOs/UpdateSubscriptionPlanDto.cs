using System.Text.Json.Serialization;
using UserService.Domain.Enums;

namespace UserService.Application.Features.SubscriptionPlans.DTOs;

public class UpdateSubscriptionPlanDto
{
    [JsonIgnore]
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "VND";
    public int DurationDays { get; set; }
    public int QuotaLimit { get; set; }
    public List<string> Features { get; set; } = new();
    public bool IsActive { get; set; }
}
