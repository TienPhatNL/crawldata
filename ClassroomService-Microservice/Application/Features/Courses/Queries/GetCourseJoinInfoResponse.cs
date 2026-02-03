namespace ClassroomService.Application.Features.Courses.Queries;

/// <summary>
/// Response for course join information
/// </summary>
public class GetCourseJoinInfoResponse
{
    /// <summary>
    /// Indicates if the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Message describing the result
    /// </summary>
    public string Message { get; set; } = null!;

    /// <summary>
    /// Course details (same DTO shape as GetCourseResponse)
    /// </summary>
    public CourseDto? Course { get; set; }

    /// <summary>
    /// Indicates if the requesting user is enrolled in the course
    /// </summary>
    public bool? IsEnrolled { get; set; }
}
