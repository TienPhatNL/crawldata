namespace ClassroomService.Domain.DTOs;

/// <summary>
/// Response DTO for template deletion
/// </summary>
public class DeleteTemplateResponse
{
    /// <summary>
    /// Whether the deletion was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Message describing the result
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// ID of the deleted template
    /// </summary>
    public Guid? TemplateId { get; set; }
}
