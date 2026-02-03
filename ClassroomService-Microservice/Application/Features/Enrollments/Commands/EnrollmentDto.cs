using ClassroomService.Domain.Enums;

namespace ClassroomService.Application.Features.Enrollments.Commands;

public class EnrollmentDto
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public DateTime? UnenrolledAt { get; set; }
    public EnrollmentStatus Status { get; set; }
    public string? UnenrollmentReason { get; set; }
    public DateTime CreatedAt { get; set; }
}