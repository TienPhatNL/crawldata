using ClassroomService.Domain.Entities;

namespace ClassroomService.Domain.Interfaces;

/// <summary>
/// Repository for managing ConversationCrawlData entities
/// </summary>
public interface IConversationCrawlDataRepository : IRepository<ConversationCrawlData>
{
    /// <summary>
    /// Get all crawl data for a specific conversation
    /// </summary>
    Task<List<ConversationCrawlData>> GetByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get crawl data by crawl job ID
    /// </summary>
    Task<ConversationCrawlData?> GetByCrawlJobIdAsync(Guid crawlJobId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Search for relevant crawl data using vector similarity
    /// </summary>
    /// <param name="conversationId">Conversation to search within</param>
    /// <param name="queryEmbedding">Query vector embedding</param>
    /// <param name="topK">Number of top results to return</param>
    /// <param name="minQuality">Minimum data quality score (0.0 to 1.0)</param>
    Task<List<ConversationCrawlData>> VectorSearchAsync(
        Guid conversationId, 
        float[] queryEmbedding, 
        int topK = 5,
        double minQuality = 0.0,
        CancellationToken cancellationToken = default);
}
