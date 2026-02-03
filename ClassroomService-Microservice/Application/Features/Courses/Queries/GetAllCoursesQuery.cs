using MediatR;
using System.ComponentModel.DataAnnotations;

namespace ClassroomService.Application.Features.Courses.Queries;

/// <summary>
/// Query to get all courses with filtering and pagination (Admin/Staff only)
/// </summary>
public class GetAllCoursesQuery : IRequest<GetAllCoursesResponse>
{
    /// <summary>
    /// Filter criteria for courses
    /// </summary>
    public CourseFilterDto Filter { get; set; } = new();

    /// <summary>
    /// Current user ID for access control (optional - can be null for anonymous access)
    /// </summary>
    public Guid? CurrentUserId { get; set; }

    /// <summary>
    /// Current user role for access control (optional)
    /// </summary>
    public string? CurrentUserRole { get; set; }
}