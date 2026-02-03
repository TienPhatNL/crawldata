using ClassroomService.Domain.Enums;

namespace ClassroomService.Domain.DTOs;

/// <summary>
/// DTO for crawler chat messages
/// </summary>
public class CrawlerChatMessageDto
{
    public Guid MessageId { get; set; }
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Guid? GroupId { get; set; }
    public Guid? AssignmentId { get; set; }
    public MessageType MessageType { get; set; } = MessageType.UserMessage;
    public Guid? CrawlJobId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? ExtractedData { get; set; }
    public string? VisualizationData { get; set; }
    
    /// <summary>
    /// Optional maximum number of pages to crawl (1-500). When provided, takes priority over prompt extraction.
    /// </summary>
    public int? MaxPages { get; set; }
}
