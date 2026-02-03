using ClassroomService.Domain.Enums;

namespace ClassroomService.Application.Features.Reports.DTOs;

public class ReportSubmissionDto
{
    public Guid Id { get; set; }
    public Guid AssignmentId { get; set; }
    public Guid? GroupId { get; set; }
    public string? GroupName { get; set; }
    public Guid SubmittedBy { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string Submission { get; set; } = string.Empty;
    public ReportStatus Status { get; set; }
    public decimal? Grade { get; set; }
    public string? Feedback { get; set; }
    public bool IsGroupSubmission { get; set; }
    public int Version { get; set; }
}
