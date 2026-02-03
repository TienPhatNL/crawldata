using MediatR;

namespace ClassroomService.Application.Features.Groups.Commands;

public class DeleteGroupCommand : IRequest<DeleteGroupResponse>
{
    public Guid GroupId { get; set; }
}
