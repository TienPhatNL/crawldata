namespace ClassroomService.Domain.Entities;

/// <summary>
/// Represents a template file that can be uploaded by staff and downloaded by lecturers
/// </summary>
public class TemplateFile
{
    /// <summary>
    /// Unique identifier for the template file
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Original file name provided during upload
    /// </summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// Stored file name (typically includes GUID for uniqueness)
    /// </summary>
    public string StoredFileName { get; set; } = string.Empty;

    /// <summary>
    /// Full file path where the template is stored
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// MIME content type (should be application/vnd.openxmlformats-officedocument.wordprocessingml.document)
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// User ID of the staff member who uploaded the template
    /// </summary>
    public string UploadedBy { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the template was uploaded
    /// </summary>
    public DateTime UploadedAt { get; set; }

    /// <summary>
    /// Timestamp when the template was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Indicates if this is the currently active template (only one can be active)
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Optional description of the template
    /// </summary>
    public string? Description { get; set; }
}
