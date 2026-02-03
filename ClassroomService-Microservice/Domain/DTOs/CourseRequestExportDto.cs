using ClassroomService.Domain.Enums;

namespace ClassroomService.Domain.DTOs;

/// <summary>
/// Domain-level DTO for course request export
/// </summary>
public class CourseRequestExportDto
{
    public Guid Id { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseCodeTitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Term { get; set; } = string.Empty;
    public string LecturerName { get; set; } = string.Empty;
    public CourseRequestStatus Status { get; set; }
    public string? RequestReason { get; set; }
    public string? ProcessedByName { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ProcessingComments { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Department { get; set; } = string.Empty;
    public Guid? CreatedCourseId { get; set; }
}