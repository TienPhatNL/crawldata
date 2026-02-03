using MediatR;

namespace ClassroomService.Application.Features.Courses.Queries;

/// <summary>
/// Query to get course access code (lecturer only)
/// </summary>
public class GetCourseAccessCodeQuery : IRequest<GetCourseAccessCodeResponse>
{
    /// <summary>
    /// Course ID
    /// </summary>
    public Guid CourseId { get; set; }

    /// <summary>
    /// Lecturer ID (set from current user context)
    /// </summary>
    public Guid LecturerId { get; set; }
}