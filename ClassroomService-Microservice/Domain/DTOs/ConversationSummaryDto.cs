namespace ClassroomService.Domain.DTOs;

public class CrawlJobDetailsDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = null!;
    public string? UserPrompt { get; set; }
    public string? ConversationName { get; set; }
    public int ResultCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class ConversationSummaryDto
{
    public Guid ConversationId { get; set; }
    public string? ConversationName { get; set; }
    public int MessageCount { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public List<string> ParticipantNames { get; set; } = new();
}
