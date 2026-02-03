using MediatR;

namespace ClassroomService.Application.Features.Groups.Queries;

/// <summary>
/// Query to get all groups in a course
/// </summary>
public class GetCourseGroupsQuery : IRequest<GetCourseGroupsResponse>
{
    public Guid CourseId { get; set; }
}
