using ClassroomService.Application.Features.Courses.Queries;

namespace ClassroomService.Application.Features.CourseCodes.Queries;

/// <summary>
/// Response for GetCourseCodeCourses query
/// </summary>
public class GetCourseCodeCoursesResponse
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
    /// List of courses for the course code
    /// </summary>
    public List<CourseDto> Courses { get; set; } = new();

    /// <summary>
    /// Total number of courses for this course code
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int CurrentPage { get; set; }

    /// <summary>
    /// Page size
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