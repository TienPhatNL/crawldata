using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Application.Features.Assignments.DTOs;

/// <summary>
/// Lightweight assignment DTO for list views
/// </summary>
public class AssignmentSummaryDto
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public Guid TopicId { get; set; }
    public string TopicName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ExtendedDueDate { get; set; }
    public AssignmentStatus Status { get; set; }
    public string StatusDisplay { get; set; } = string.Empty;
    public bool IsGroupAssignment { get; set; }
    public int? MaxPoints { get; set; }
    
    /// <summary>
    /// Resolved weight percentage for this assignment's topic (0-100)
    /// Priority: SpecificCourse > CourseCode > 0
    /// </summary>
    public decimal? WeightPercentage { get; set; }
    
    // Assigned group IDs (for group assignments)
    public List<Guid>? GroupIds { get; set; }
    
    /// <summary>
    /// List of file attachments (instructions, reference materials, etc.)
    /// </summary>
    public List<AttachmentMetadata>? Attachments { get; set; }
    
    // Computed properties
    public bool IsOverdue { get; set; }
    public int DaysUntilDue { get; set; }
    public int AssignedGroupsCount { get; set; }
    
    public DateTime CreatedAt { get; set; }
}
