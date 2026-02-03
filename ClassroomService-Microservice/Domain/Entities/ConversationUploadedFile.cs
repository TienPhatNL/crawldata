using ClassroomService.Domain.Common;

namespace ClassroomService.Domain.Entities;

/// <summary>
/// Represents an uploaded CSV file in a conversation
/// Stores parsed CSV data as JSON for querying and analysis
/// </summary>
public class ConversationUploadedFile : BaseAuditableEntity
{
    /// <summary>
    /// Reference to the conversation this file belongs to
    /// </summary>
    public Guid ConversationId { get; set; }
    
    /// <summary>
    /// Original filename of the uploaded CSV file
    /// </summary>
    public string FileName { get; set; } = string.Empty;
    
    /// <summary>
    /// URL where the file is stored (S3 or other storage)
    /// </summary>
    public string FileUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Size of the file in bytes
    /// </summary>
    public long FileSize { get; set; }
    
    /// <summary>
    /// Parsed CSV data as JSON array of objects
    /// Format: [{col1: val1, col2: val2}, ...]
    /// </summary>
    public string DataJson { get; set; } = "[]";
    
    /// <summary>
    /// Number of data rows (excluding header)
    /// </summary>
    public int RowCount { get; set; }
    
    /// <summary>
    /// Column names from CSV header as JSON array
    /// Format: ["col1", "col2", ...]
    /// </summary>
    public string ColumnNamesJson { get; set; } = "[]";
    
    /// <summary>
    /// Timestamp when the file was uploaded
    /// </summary>
    public DateTime UploadedAt { get; set; }
    
    /// <summary>
    /// User who uploaded the file
    /// </summary>
    public Guid UploadedBy { get; set; }
    
    /// <summary>
    /// Soft delete flag
    /// </summary>
    public bool IsDeleted { get; set; } = false;
    
    // Navigation property
    public virtual Conversation Conversation { get; set; } = null!;
}
