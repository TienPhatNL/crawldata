using MediatR;

namespace ClassroomService.Application.Features.Courses.Queries;

/// <summary>
/// Query to get course details for joining (public information)
/// </summary>
public class GetCourseJoinInfoQuery : IRequest<GetCourseJoinInfoResponse>
{
    /// <summary>
    /// The ID of the course
    /// </summary>
    public Guid CourseId { get; set; }

    /// <summary>
    /// Optional: ID of the user checking (for enrollment status)
    /// </summary>
    public Guid? UserId { get; set; }
}