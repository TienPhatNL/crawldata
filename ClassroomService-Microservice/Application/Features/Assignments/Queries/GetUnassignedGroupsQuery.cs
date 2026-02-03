using MediatR;

namespace ClassroomService.Application.Features.Assignments.Queries;

public class GetUnassignedGroupsQuery : IRequest<GetUnassignedGroupsResponse>
{
    public Guid CourseId { get; set; }
}
