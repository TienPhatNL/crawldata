using System.ComponentModel.DataAnnotations;

namespace UserService.Application.Features.SubscriptionTiers.DTOs;

public class UpdateSubscriptionTierDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;
    
    [Required, MaxLength(500)]
    public string Description { get; set; } = null!;
    
    [Range(0, 100)]
    public int Level { get; set; }
    
    public bool IsActive { get; set; } = true;
}
