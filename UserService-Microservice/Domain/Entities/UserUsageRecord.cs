using UserService.Domain.Common;
using UserService.Domain.Enums;

namespace UserService.Domain.Entities;

public class UserUsageRecord : BaseEntity
{
    public Guid UserId { get; set; }
    public UsageType UsageType { get; set; }
    public int Quantity { get; set; } = 1;
    public DateTime UsageDate { get; set; }
    public string? ResourceId { get; set; } // Job ID, Report ID, etc.
    public string? Metadata { get; set; } // JSON string for additional data
    public decimal? Cost { get; set; }
    public string Currency { get; set; } = "USD";
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
}