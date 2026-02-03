namespace ClassroomService.Application.Features.Enrollments.Queries;

/// <summary>
/// Response containing enrolled students for a course
/// </summary>
public class GetCourseEnrolledStudentsResponse
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Response message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Course ID
    /// </summary>
    public Guid CourseId { get; set; }

    /// <summary>
    /// Course name
    /// </summary>
    public string CourseName { get; set; } = string.Empty;

    /// <summary>
    /// List of enrolled students
    /// </summary>
    public List<EnrolledStudentDto> Students { get; set; } = new List<EnrolledStudentDto>();

    /// <summary>
    /// Total number of enrolled students
    /// </summary>
    public int TotalStudents { get; set; }
}

/// <summary>
/// DTO representing an enrolled student
/// </summary>
public class EnrolledStudentDto
{
    /// <summary>
    /// Student user ID
    /// </summary>
    public Guid StudentId { get; set; }

    /// <summary>
    /// Student email
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Student first name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Student last name
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Student full name
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Student ID number
    /// </summary>
    public string? StudentIdNumber { get; set; }

    /// <summary>
    /// Profile picture URL
    /// </summary>
    public string? ProfilePictureUrl { get; set; }

    /// <summary>
    /// When the student joined the course
    /// </summary>
    public DateTime JoinedAt { get; set; }

    /// <summary>
    /// Enrollment status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Enrollment ID
    /// </summary>
    public Guid EnrollmentId { get; set; }
}
