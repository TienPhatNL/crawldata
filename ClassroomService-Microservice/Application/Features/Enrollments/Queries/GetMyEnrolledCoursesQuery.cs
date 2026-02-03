using MediatR;

namespace ClassroomService.Application.Features.Enrollments.Queries;

/// <summary>
/// Query to get current user's enrolled courses
/// </summary>
public class GetMyEnrolledCoursesQuery : IRequest<GetMyEnrolledCoursesResponse>
{
    /// <summary>
    /// The student ID (set from current user context)
    /// </summary>
    public Guid StudentId { get; set; }
}
