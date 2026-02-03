namespace ClassroomService.Application.Features.Enrollments.Commands;

/// <summary>
/// Response for unenrolling a student from a course
/// </summary>
public class UnenrollStudentResponse
{
    /// <summary>
    /// Indicates if the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Message describing the result
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The unenrolled student information (if successful)
    /// </summary>
    public EnrollmentDto? UnenrolledStudent { get; set; }
}