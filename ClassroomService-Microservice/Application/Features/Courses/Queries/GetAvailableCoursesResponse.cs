using ClassroomService.Application.Features.Courses.Queries;

namespace ClassroomService.Application.Features.Courses.Queries;

/// <summary>
/// Response for getting available courses for students to browse
/// </summary>
public class GetAvailableCoursesResponse
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
    /// List of available courses (without sensitive information like access codes)
    /// </summary>
    public List<AvailableCourseDto> Courses { get; set; } = new List<AvailableCourseDto>();

    /// <summary>
    /// Total number of courses matching the filter
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int CurrentPage { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage { get; set; }

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage { get; set; }
}

/// <summary>
/// Public course information for students to browse (without sensitive data)
/// </summary>
public class AvailableCourseDto
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
    /// Course description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Lecturer ID
    /// </summary>
    public Guid LecturerId { get; set; }

    /// <summary>
    /// Lecturer name
    /// </summary>
    public string LecturerName { get; set; } = string.Empty;

    /// <summary>
    /// When the course was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Number of students enrolled
    /// </summary>
    public int EnrollmentCount { get; set; }

    /// <summary>
    /// Whether this course requires an access code to join
    /// </summary>
    public bool RequiresAccessCode { get; set; }

    /// <summary>
    /// Whether the access code is expired (if applicable)
    /// </summary>
    public bool IsAccessCodeExpired { get; set; }

    /// <summary>
    /// Optional course image URL or path
    /// </summary>
    public string? Img { get; set; }

    /// <summary>
    /// Course unique code for enrollment
    /// </summary>
    public string UniqueCode { get; set; } = string.Empty;

    /// <summary>
    /// Lecturer's profile image
    /// </summary>
    public string? LecturerImage { get; set; }

    /// <summary>
    /// Term name
    /// </summary>
    public string TermName { get; set; } = string.Empty;

    /// <summary>
    /// Term start date
    /// </summary>
    public DateTime? TermStartDate { get; set; }

    /// <summary>
    /// Term end date
    /// </summary>
    public DateTime? TermEndDate { get; set; }

    /// <summary>
    /// User's enrollment status in this course (if user is authenticated)
    /// </summary>
    public UserCourseEnrollmentStatus? EnrollmentStatus { get; set; }

    /// <summary>
    /// Whether the user can join this course
    /// </summary>
    public bool CanJoin { get; set; } = true;

    /// <summary>
    /// Join URL for this course
    /// </summary>
    public string JoinUrl { get; set; } = string.Empty;
}

/// <summary>
/// User's enrollment status for a specific course
/// </summary>
public class UserCourseEnrollmentStatus
{
    /// <summary>
    /// Whether the user is currently enrolled
    /// </summary>
    public bool IsEnrolled { get; set; }

    /// <summary>
    /// When the user joined (if applicable)
    /// </summary>
    public DateTime? JoinedAt { get; set; }

    /// <summary>
    /// Enrollment status
    /// </summary>
    public string? Status { get; set; }
}