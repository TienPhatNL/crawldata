namespace ClassroomService.Domain.DTOs;

/// <summary>
/// DTO for template metadata without file content
/// </summary>
public class TemplateMetadataDto
{
    /// <summary>
    /// Whether an active template exists
    /// </summary>
    public bool HasActiveTemplate { get; set; }

    /// <summary>
    /// Template file information (null if no active template)
    /// </summary>
    public TemplateFileDto? Template { get; set; }

    /// <summary>
    /// Message for the user
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
