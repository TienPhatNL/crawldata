using MediatR;
using System.Text.Json.Serialization;

namespace ClassroomService.Application.Features.Assignments.Commands;

public class UnassignGroupsCommand : IRequest<UnassignGroupsResponse>
{
    public Guid AssignmentId { get; set; }
    public List<Guid> GroupIds { get; set; } = new();
    
    [JsonIgnore]
    public Guid? UnassignedBy { get; set; }
}
