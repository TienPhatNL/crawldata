using MediatR;
using System.Text.Json.Serialization;

namespace ClassroomService.Application.Features.GroupMembers.Commands;

/// <summary>
/// Command to assign a new leader to a group
/// </summary>
public class AssignGroupLeaderCommand : IRequest<AssignGroupLeaderResponse>
{
    /// <summary>
    /// Group ID from route parameter
    /// </summary>
    [JsonIgnore]
    public Guid GroupId { get; set; }
    
    public Guid StudentId { get; set; }
}
