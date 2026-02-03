using ClassroomService.Domain.Enums;

namespace ClassroomService.Application.Features.Reports.DTOs;

public class ReportDto
{
    public Guid Id { get; set; }
    public Guid AssignmentId { get; set; }
    public string AssignmentTitle { get; set; } = string.Empty;
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public Guid? GroupId { get; set; }
    public string? GroupName { get; set; }
    public Guid SubmittedBy { get; set; }
    public string? SubmittedByName { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public ReportStatus Status { get; set; }
    public decimal? Grade { get; set; }
    public string? Feedback { get; set; }
    public Guid? GradedBy { get; set; }
    public string? GradedByName { get; set; }
    public DateTime? GradedAt { get; set; }
    public bool IsGroupSubmission { get; set; }
    public int Version { get; set; }
    public string? FileUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
