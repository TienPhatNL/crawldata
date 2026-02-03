namespace ClassroomService.Application.Features.CrawlerChat.Commands;

public class InitiateCrawlFromChatResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? MessageId { get; set; }
    public Guid? CrawlJobId { get; set; }
}
