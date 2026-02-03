using ClassroomService.Application.Features.Enrollments.Commands;

namespace ClassroomService.Application.Features.Enrollments.Queries;

/// <summary>
/// Response for getting enrolled courses
/// </summary>
public class GetMyEnrolledCoursesResponse
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
    /// List of enrolled courses
    /// </summary>
    public List<EnrolledCourseDto> Courses { get; set; } = new();

    /// <summary>
    /// Total number of enrolled courses
    /// </summary>
    public int TotalCount { get; set; }
}

/// <summary>
/// DTO for enrolled course information
/// </summary>
public class EnrolledCourseDto
{
    /// <summary>
    /// Course ID
    /// </summary>
    public Guid CourseId { get; set; }

    /// <summary>
    /// Course code (e.g., "CS101")
    /// </summary>
    public string CourseCode { get; set; } = string.Empty;

    /// <summary>
    /// Course name
    /// </summary>
    public string CourseName { get; set; } = string.Empty;

    /// <summary>
    /// Course description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Lecturer name
    /// </summary>
    public string LecturerName { get; set; } = string.Empty;

    /// <summary>
    /// Academic term
    /// </summary>
    public string Term { get; set; } = string.Empty;

    /// <summary>
    /// When the student joined the course
    /// </summary>
    public DateTime JoinedAt { get; set; }

    /// <summary>
    /// Enrollment ID
    /// </summary>
    public Guid EnrollmentId { get; set; }

    /// <summary>
    /// Number of students enrolled in the course
    /// </summary>
    public int EnrollmentCount { get; set; }

    /// <summary>
    /// Department (if available)
    /// </summary>
    public string? Department { get; set; }

    /// <summary>
    /// Optional course image URL or path
    /// </summary>
    public string? Img { get; set; }
}
