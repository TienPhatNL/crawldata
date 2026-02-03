using ClassroomService.Domain.DTOs;

namespace ClassroomService.Application.Features.Assignments.DTOs;

/// <summary>
/// Detailed assignment DTO with full information including assigned groups
/// </summary>
public class AssignmentDetailDto : AssignmentSummaryDto
{
    public string Description { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// List of file attachments (instructions, reference materials, etc.)
    /// </summary>
    public List<AttachmentMetadata>? Attachments { get; set; }
    
    // Navigation
    public List<GroupDto>? AssignedGroups { get; set; }
}
