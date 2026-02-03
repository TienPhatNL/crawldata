namespace ClassroomService.Domain.DTOs;

public class InitiateCrawlRequest
{
    public required string Url { get; set; }
    public required string Prompt { get; set; }
    public Guid UserId { get; set; }
    public Guid AssignmentId { get; set; }
    public Guid? GroupId { get; set; }
    public Guid? ConversationThreadId { get; set; }
    public CrawlerAgentType AgentType { get; set; } = CrawlerAgentType.Smart;
    public bool EnableAiExtraction { get; set; } = true;
    public Dictionary<string, string>? AdditionalHeaders { get; set; }
    public int? MaxDepth { get; set; }
    public bool EnableJavaScript { get; set; } = false;
}

public enum CrawlerAgentType
{
    Smart = 0,
    Http = 1,
    Selenium = 2,
    Playwright = 3,
    Scrapy = 4,
    Crawl4Ai = 5
}
