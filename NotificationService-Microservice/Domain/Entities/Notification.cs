using NotificationService.Domain.Common;
using NotificationService.Domain.Enums;

namespace NotificationService.Domain.Entities;

public class Notification : BaseAuditableEntity, ISoftDelete
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public NotificationPriority Priority { get; set; }
    public EventSource Source { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    
    /// <summary>
    /// Store structured metadata as JSON string containing all necessary IDs for frontend URL construction
    /// Example: {"CourseId":"...", "AssignmentId":"...", "GroupId":"...", "ReportId":"..."}
    /// </summary>
    public string? MetadataJson { get; set; }
    
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<NotificationDeliveryLog> DeliveryLogs { get; set; } = new List<NotificationDeliveryLog>();
}
