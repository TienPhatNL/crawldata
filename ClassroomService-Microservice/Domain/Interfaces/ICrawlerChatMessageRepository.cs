using ClassroomService.Domain.Entities;

namespace ClassroomService.Domain.Interfaces;

/// <summary>
/// Repository for managing CrawlerChatMessage entities
/// </summary>
public interface ICrawlerChatMessageRepository : IRepository<CrawlerChatMessage>
{
    /// <summary>
    /// Gets a message by its ID
    /// </summary>
    Task<CrawlerChatMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all messages for a conversation
    /// </summary>
    Task<List<CrawlerChatMessage>> GetByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a message by associated crawl job ID
    /// </summary>
    Task<CrawlerChatMessage?> GetByCrawlJobIdAsync(Guid crawlJobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all messages for an assignment
    /// </summary>
    Task<List<CrawlerChatMessage>> GetByAssignmentIdAsync(Guid assignmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all messages for a group
    /// </summary>
    Task<List<CrawlerChatMessage>> GetByGroupIdAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets conversation history with pagination
    /// </summary>
    Task<List<CrawlerChatMessage>> GetConversationHistoryAsync(
        Guid conversationId,
        int limit = 50,
        int offset = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks messages as read for a user
    /// </summary>
    Task MarkAsReadAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);
}
