namespace ClassroomService.Domain.DTOs;

public class SmartCrawlRequest
{
    public required string Url { get; set; }
    public required string Prompt { get; set; }
    public Guid UserId { get; set; }
    public Guid? AssignmentId { get; set; }
    public Guid? GroupId { get; set; }
    public Guid? ConversationThreadId { get; set; }
}
