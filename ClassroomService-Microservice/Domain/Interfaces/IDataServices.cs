using ClassroomService.Domain.DTOs;

namespace ClassroomService.Domain.Interfaces;

/// <summary>
/// Service for validating and cleaning crawl data
/// </summary>
public interface IDataValidator
{
    /// <summary>
    /// Validate and clean raw extracted data
    /// </summary>
    /// <param name="rawData">Raw data from crawl results</param>
    /// <returns>Validation result with cleaned data and warnings</returns>
    Task<ValidationResult> ValidateAndCleanAsync(object rawData);
    
    /// <summary>
    /// Detect schema from a list of records
    /// </summary>
    /// <param name="records">List of data records</param>
    /// <returns>Detected schema information</returns>
    DataSchema DetectSchema(List<object> records);
}

/// <summary>
/// Service for generating and managing vector embeddings
/// </summary>
public interface IVectorEmbeddingService
{
    /// <summary>
    /// Generate vector embedding for text
    /// </summary>
    /// <param name="text">Text to embed</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Vector embedding as float array</returns>
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Calculate cosine similarity between two vectors
    /// </summary>
    /// <param name="vector1">First vector</param>
    /// <param name="vector2">Second vector</param>
    /// <returns>Similarity score (0.0 to 1.0)</returns>
    double CosineSimilarity(float[] vector1, float[] vector2);
}

/// <summary>
/// Service for normalizing and storing crawl data
/// </summary>
public interface ICrawlDataNormalizationService
{
    /// <summary>
    /// Normalize crawl data and store in conversation context
    /// </summary>
    /// <param name="conversationId">Conversation ID</param>
    /// <param name="crawlJobId">Crawl job ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Normalized conversation crawl data</returns>
    Task<Entities.ConversationCrawlData> NormalizeAndStoreAsync(
        Guid conversationId,
        Guid crawlJobId,
        CancellationToken cancellationToken = default);
}
