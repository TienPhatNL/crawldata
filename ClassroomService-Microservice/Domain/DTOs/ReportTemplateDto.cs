namespace ClassroomService.Domain.DTOs;

/// <summary>
/// Response DTO for report template preview
/// </summary>
public class ReportTemplateDto
{
    /// <summary>
    /// HTML content of the template
    /// </summary>
    public string HtmlContent { get; set; } = string.Empty;

    /// <summary>
    /// Name of the template
    /// </summary>
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// Template version
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Last modification date of the template
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Maximum allowed length of the report content (characters)
    /// </summary>
    public int MaxLength { get; set; }

    /// <summary>
    /// Template description
    /// </summary>
    public string? Description { get; set; }
}
