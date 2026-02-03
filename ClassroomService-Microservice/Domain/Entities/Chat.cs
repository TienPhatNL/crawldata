using ClassroomService.Domain.Common;

namespace ClassroomService.Domain.Entities;

public class Chat : BaseAuditableEntity
{
    public Guid SenderId { get; set; }
    public Guid ReceiverId { get; set; }
    public Guid CourseId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    
    // Conversation Threading
    public Guid ConversationId { get; set; }
    
    // Support Request Link (to isolate messages per support request)
    // NULL = general chat, NOT NULL = support request specific message
    public Guid? SupportRequestId { get; set; }
    
    // Soft Delete
    public bool IsDeleted { get; set; } = false;
    
    // Read Tracking
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }

    // Navigation properties
    public virtual Course Course { get; set; } = null!;
    public virtual Conversation Conversation { get; set; } = null!;
    public virtual SupportRequest? SupportRequest { get; set; }
}