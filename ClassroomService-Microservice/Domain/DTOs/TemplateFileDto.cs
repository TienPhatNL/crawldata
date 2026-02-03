namespace ClassroomService.Domain.DTOs;

/// <summary>
/// DTO for template file information
/// </summary>
public class TemplateFileDto
{
    /// <summary>
    /// Unique identifier for the template file
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Original file name
    /// </summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// File size formatted as human-readable string (e.g., "2.5 MB")
    /// </summary>
    public string FileSizeFormatted { get; set; } = string.Empty;

    /// <summary>
    /// User who uploaded the template
    /// </summary>
    public string UploadedBy { get; set; } = string.Empty;

    /// <summary>
    /// Upload timestamp
    /// </summary>
    public DateTime UploadedAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Whether this template is currently active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Template description
    /// </summary>
    public string? Description { get; set; }
}
