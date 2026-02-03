using ClassroomService.Domain.Enums;

namespace ClassroomService.Application.Features.Reports.DTOs;

public class ReportDetailDto
{
    public Guid Id { get; set; }
    public Guid AssignmentId { get; set; }
    public string AssignmentTitle { get; set; } = string.Empty;
    public string AssignmentDescription { get; set; } = string.Empty;
    public int? AssignmentMaxPoints { get; set; }
    public DateTime AssignmentDueDate { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    
    public Guid? GroupId { get; set; }
    public string? GroupName { get; set; }
    public Guid SubmittedBy { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string Submission { get; set; } = string.Empty;
    public ReportStatus Status { get; set; }
    public decimal? Grade { get; set; }
    public string? Feedback { get; set; }
    public Guid? GradedBy { get; set; }
    public DateTime? GradedAt { get; set; }
    public bool IsGroupSubmission { get; set; }
    public int Version { get; set; }
    public string? FileUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
