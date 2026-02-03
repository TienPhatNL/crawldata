using ClassroomService.Domain.Common;

namespace ClassroomService.Domain.Entities;

/// <summary>
/// Aggregated and normalized crawl data at conversation level for RAG queries
/// Enables semantic search across multiple crawl jobs within a conversation
/// </summary>
public class ConversationCrawlData : BaseAuditableEntity
{
    /// <summary>
    /// Reference to the conversation this data belongs to
    /// </summary>
    public Guid ConversationId { get; set; }
    
    /// <summary>
    /// Reference to the original crawl job in WebCrawlerService
    /// </summary>
    public Guid CrawlJobId { get; set; }
    
    // Source metadata
    
    /// <summary>
    /// Source URL that was crawled
    /// </summary>
    public string SourceUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp when the crawl was performed
    /// </summary>
    public DateTime CrawledAt { get; set; }
    
    /// <summary>
    /// Total number of results from the crawl job
    /// </summary>
    public int ResultCount { get; set; }
    
    // Normalized extracted data
    
    /// <summary>
    /// Cleaned and normalized JSON data with validation errors removed
    /// Structure: {"products": [...], "validation_warnings": [...]}
    /// </summary>
    public string NormalizedDataJson { get; set; } = "{}";
    
    // Vector embedding for RAG (semantic search)
    
    /// <summary>
    /// Text summary for embedding generation
    /// Format: "Source: {url}\nData: {summary}\nFields: {fieldNames}"
    /// </summary>
    public string EmbeddingText { get; set; } = string.Empty;
    
    /// <summary>
    /// Vector embedding as JSON array (1536 dimensions for OpenAI, 768 for Gemini)
    /// Stored as JSON until we migrate to PostgreSQL with pgvector
    /// Example: "[0.123, -0.456, ...]"
    /// </summary>
    public string? VectorEmbeddingJson { get; set; }
    
    // Data quality metrics
    
    /// <summary>
    /// Number of valid records after normalization
    /// </summary>
    public int ValidRecordCount { get; set; }
    
    /// <summary>
    /// Number of invalid/skipped records
    /// </summary>
    public int InvalidRecordCount { get; set; }
    
    /// <summary>
    /// Data quality score (0.0 to 1.0)
    /// Calculated as ValidRecordCount / (ValidRecordCount + InvalidRecordCount)
    /// </summary>
    public double DataQualityScore { get; set; }
    
    /// <summary>
    /// Validation warnings during data normalization
    /// Stored as JSON array: ["Warning 1", "Warning 2"]
    /// </summary>
    public string? ValidationWarningsJson { get; set; }
    
    /// <summary>
    /// Soft delete flag
    /// </summary>
    public bool IsDeleted { get; set; }
    
    // Schema detection
    
    /// <summary>
    /// Detected schema of the data (auto-inferred)
    /// Example: {"type": "product_list", "fields": ["name", "price", "rating"], "labelField": "name", "valueField": "price"}
    /// </summary>
    public string DetectedSchemaJson { get; set; } = "{}";
    
    // Navigation properties
    public virtual Conversation Conversation { get; set; } = null!;
}
