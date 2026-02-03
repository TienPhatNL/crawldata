using MediatR;
using System.ComponentModel.DataAnnotations;
using ClassroomService.Application.Features.Courses.Queries;

namespace ClassroomService.Application.Features.CourseCodes.Queries;

/// <summary>
/// Query to get all courses for a specific course code
/// </summary>
public class GetCourseCodeCoursesQuery : IRequest<GetCourseCodeCoursesResponse>
{
    /// <summary>
    /// The course code ID to get courses for
    /// </summary>
    [Required]
    public Guid CourseCodeId { get; set; }

    /// <summary>
    /// Page number for pagination (1-based)
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page (max 100)
    /// </summary>
    [Range(1, 100)]
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Sort direction (asc or desc) for created date
    /// </summary>
    public string SortDirection { get; set; } = "desc";
}