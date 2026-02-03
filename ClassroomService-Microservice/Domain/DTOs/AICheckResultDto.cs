using ClassroomService.Domain.Enums;

namespace ClassroomService.Domain.DTOs;

/// <summary>
/// Result DTO for AI content check operation
/// </summary>
public class AICheckResultDto
{
    /// <summary>
    /// Unique ID of the AI check record
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// The report that was checked
    /// </summary>
    public Guid ReportId { get; set; }
    
    /// <summary>
    /// AI detection percentage (0-100)
    /// Higher values indicate more likely AI-generated
    /// </summary>
    public decimal AIPercentage { get; set; }
    
    /// <summary>
    /// AI detection service provider
    /// </summary>
    public string Provider { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the lecturer who performed the check
    /// </summary>
    public Guid CheckedBy { get; set; }
    
    /// <summary>
    /// Name/email of the lecturer who performed the check
    /// </summary>
    public string CheckedByName { get; set; } = string.Empty;
    
    /// <summary>
    /// When the check was performed
    /// </summary>
    public DateTime CheckedAt { get; set; }
    
    /// <summary>
    /// Optional notes about this check
    /// </summary>
    public string? Notes { get; set; }
    
    /// <summary>
    /// Current status of the report
    /// </summary>
    public ReportStatus ReportStatus { get; set; }
    
    /// <summary>
    /// Name of the student who submitted the report
    /// </summary>
    public string StudentName { get; set; } = string.Empty;
    
    /// <summary>
    /// Title of the assignment
    /// </summary>
    public string AssignmentTitle { get; set; } = string.Empty;
}
