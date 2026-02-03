using ClassroomService.Domain.Common;

namespace ClassroomService.Domain.Entities;

/// <summary>
/// Represents a conversation thread between two users in a course
/// </summary>
public class Conversation : BaseAuditableEntity
{
    public Guid CourseId { get; set; }
    
    // Always store in order: User1Id < User2Id (for uniqueness on non-crawler conversations)
    public Guid User1Id { get; set; }
    public Guid User2Id { get; set; }

    // True for crawler-specific conversations (allows multiple per user/course)
    public bool IsCrawler { get; set; } = false;
    
    /// <summary>
    /// AI-generated summary of the conversation topic based on the first crawl prompt
    /// </summary>
    public string? Name { get; set; }
    
    // Last message metadata
    public DateTime LastMessageAt { get; set; }
    public string LastMessagePreview { get; set; } = string.Empty;
    
    // Conversation status
    public bool IsClosed { get; set; } = false;
    public DateTime? ClosedAt { get; set; }
    public Guid? ClosedBy { get; set; }
    
    // Navigation
    public virtual Course Course { get; set; } = null!;
    public virtual ICollection<Chat> Messages { get; set; } = new List<Chat>();
}
