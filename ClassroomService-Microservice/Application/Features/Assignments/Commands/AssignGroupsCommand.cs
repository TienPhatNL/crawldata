using MediatR;
using System.Text.Json.Serialization;

namespace ClassroomService.Application.Features.Assignments.Commands;

public class AssignGroupsCommand : IRequest<AssignGroupsResponse>
{
    public Guid AssignmentId { get; set; }
    public List<Guid> GroupIds { get; set; } = new();
}
