namespace ClassroomService.Domain.DTOs;

/// <summary>
/// Request DTO for initiating an AI content check on a report
/// </summary>
public class AICheckRequestDto
{
    /// <summary>
    /// The ID of the report to check
    /// </summary>
    public Guid ReportId { get; set; }
    
    /// <summary>
    /// Optional notes about why this check is being performed
    /// </summary>
    public string? Notes { get; set; }
}
