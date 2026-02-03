using ClassroomService.Domain.Common;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Domain.Entities;

/// <summary>
/// Represents a message in the crawler chat interface for assignments
/// Enables interactive communication between students, groups, and the crawler agent
/// </summary>
public class CrawlerChatMessage : BaseAuditableEntity
{
    /// <summary>
    /// Conversation ID for threading related messages
    /// </summary>
    public Guid ConversationId { get; set; }

    /// <summary>
    /// Parent message ID for reply threading
    /// </summary>
    public Guid? ParentMessageId { get; set; }

    /// <summary>
    /// Assignment this message belongs to
    /// </summary>
    public Guid AssignmentId { get; set; }

    /// <summary>
    /// Group this message belongs to (null for individual assignments)
    /// </summary>
    public Guid? GroupId { get; set; }

    /// <summary>
    /// User who sent the message (or system for automated messages)
    /// </summary>
    public Guid SenderId { get; set; }

    /// <summary>
    /// Message content (text, prompt, or system notification)
    /// </summary>
    public string MessageContent { get; set; } = string.Empty;

    /// <summary>
    /// Type of message
    /// </summary>
    public MessageType MessageType { get; set; } = MessageType.UserMessage;

    /// <summary>
    /// Reference to WebCrawlerService job ID (if this message initiated a crawl)
    /// </summary>
    public Guid? CrawlJobId { get; set; }

    /// <summary>
    /// Cached summary of crawl results (populated when crawl completes)
    /// </summary>
    public string? CrawlResultSummary { get; set; }

    /// <summary>
    /// Whether this is a system-generated message
    /// </summary>
    public bool IsSystemMessage { get; set; } = false;

    /// <summary>
    /// Additional metadata stored as JSON (for visualizations, attachments, etc.)
    /// </summary>
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Whether the message has been read by all participants
    /// </summary>
    public bool IsRead { get; set; } = false;

    /// <summary>
    /// Timestamp when message was edited (null if never edited)
    /// </summary>
    public DateTime? EditedAt { get; set; }

    // Navigation properties
    public virtual Assignment Assignment { get; set; } = null!;
    public virtual Group? Group { get; set; }
    public virtual CrawlerChatMessage? ParentMessage { get; set; }
    public virtual ICollection<CrawlerChatMessage> Replies { get; set; } = new List<CrawlerChatMessage>();
}
