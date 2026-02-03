using ClassroomService.Domain.Enums;

namespace ClassroomService.Application.Features.Enrollments.Queries;

/// <summary>
/// Response for enrollment status check
/// </summary>
public class EnrollmentStatusResponse
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
    /// Whether the student is enrolled in the course
    /// </summary>
    public bool IsEnrolled { get; set; }

    /// <summary>
    /// The enrollment status if enrolled
    /// </summary>
    public EnrollmentStatus? Status { get; set; }

    /// <summary>
    /// When the student joined the course (if enrolled)
    /// </summary>
    public DateTime? JoinedAt { get; set; }

    /// <summary>
    /// Course information
    /// </summary>
    public CourseInfo? Course { get; set; }
}

/// <summary>
/// Basic course information for enrollment status
/// </summary>
public class CourseInfo
{
    /// <summary>
    /// Course ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Course code
    /// </summary>
    public string CourseCode { get; set; } = string.Empty;

    /// <summary>
    /// Course name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Lecturer name
    /// </summary>
    public string LecturerName { get; set; } = string.Empty;
}