using MediatR;

namespace ClassroomService.Application.Features.Courses.Queries;

/// <summary>
/// Query to get publicly available courses for students to browse and join
/// </summary>
public class GetAvailableCoursesQuery : IRequest<GetAvailableCoursesResponse>
{
    /// <summary>
    /// Filter parameters for course search
    /// </summary>
    public CourseFilterDto Filter { get; set; } = new CourseFilterDto();
    
    /// <summary>
    /// Current user ID (optional) to check enrollment status
    /// </summary>
    public Guid? UserId { get; set; }
}