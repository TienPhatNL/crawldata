using NotificationService.Domain.Common;
using NotificationService.Domain.Enums;

namespace NotificationService.Domain.Entities;

public class NotificationDeliveryLog : BaseEntity
{
    public Guid NotificationId { get; set; }
    public NotificationChannel Channel { get; set; }
    public DeliveryStatus Status { get; set; }
    public DateTime AttemptedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    
    // Navigation properties
    public virtual Notification Notification { get; set; } = null!;
}
