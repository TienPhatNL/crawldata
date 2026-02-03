using MediatR;

namespace ClassroomService.Application.Features.Groups.Queries;

/// <summary>
/// Query to get a group by ID
/// </summary>
public class GetGroupByIdQuery : IRequest<GetGroupByIdResponse>
{
    public Guid GroupId { get; set; }
}
