using MediatR;

namespace ClassroomService.Application.Features.Enrollments.Queries;

/// <summary>
/// Query to get all enrolled students in a specific course
/// </summary>
public class GetCourseEnrolledStudentsQuery : IRequest<GetCourseEnrolledStudentsResponse>
{
    /// <summary>
    /// The course ID to get enrolled students for
    /// </summary>
    public Guid CourseId { get; set; }

    /// <summary>
    /// The user making the request (for authorization)
    /// </summary>
    public Guid RequestedBy { get; set; }
}
