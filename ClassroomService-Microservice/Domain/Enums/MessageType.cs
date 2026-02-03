namespace ClassroomService.Domain.Enums;

/// <summary>
/// Types of messages in the crawler chat system
/// </summary>
public enum MessageType
{
    /// <summary>
    /// Regular user message/chat
    /// </summary>
    UserMessage = 0,

    /// <summary>
    /// User requesting a crawl operation
    /// </summary>
    CrawlRequest = 1,

    /// <summary>
    /// System notification about crawl results
    /// </summary>
    CrawlResult = 2,

    /// <summary>
    /// System notification (job started, completed, failed, etc.)
    /// </summary>
    SystemNotification = 3,

    /// <summary>
    /// Data visualization generated from crawl results
    /// </summary>
    Visualization = 4,

    /// <summary>
    /// AI-generated summary or insight
    /// </summary>
    AiSummary = 5,

    /// <summary>
    /// User asking follow-up question about crawl data
    /// </summary>
    FollowUpQuestion = 6
}
