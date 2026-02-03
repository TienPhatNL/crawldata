using MediatR;
using System.ComponentModel.DataAnnotations;

namespace ClassroomService.Application.Features.Courses.Queries;

/// <summary>
/// Enhanced command to get user's courses with filtering and pagination
/// </summary>
public class GetMyCoursesCommand : IRequest<GetMyCoursesResponse>
{
    /// <summary>
    /// User ID (will be set automatically from current user)
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Whether to get courses as lecturer (true) or student (false)
    /// </summary>
    /// <example>false</example>
    [Required]
    public bool AsLecturer { get; set; } = false;

    /// <summary>
    /// Filter criteria for courses
    /// </summary>
    public CourseFilterDto Filter { get; set; } = new();
}