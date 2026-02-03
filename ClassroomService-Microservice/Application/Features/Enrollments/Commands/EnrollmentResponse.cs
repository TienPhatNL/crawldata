namespace ClassroomService.Application.Features.Enrollments.Commands;

/// <summary>
/// Response for enrollment operations (join course, self-enrollment)
/// </summary>
public class EnrollmentResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? EnrollmentId { get; set; }
    public EnrollmentDto? Enrollment { get; set; }
}
