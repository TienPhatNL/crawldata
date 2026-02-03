namespace ClassroomService.Domain.DTOs;

/// <summary>
/// Represents metadata for a single file attachment
/// </summary>
public class AttachmentMetadata
{
    /// <summary>
    /// Unique identifier for the attachment
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Original file name
    /// </summary>
    public string FileName { get; set; } = string.Empty;
    
    /// <summary>
    /// Full URL to the uploaded file (S3, Azure Blob, etc.)
    /// </summary>
    public string FileUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }
    
    /// <summary>
    /// MIME content type (e.g., "application/pdf")
    /// </summary>
    public string ContentType { get; set; } = string.Empty;
    
    /// <summary>
    /// When the file was uploaded
    /// </summary>
    public DateTime UploadedAt { get; set; }
    
    /// <summary>
    /// User ID who uploaded the file
    /// </summary>
    public Guid UploadedBy { get; set; }
}
