using MediatR;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace ClassroomService.Application.Features.Groups.Commands;

/// <summary>
/// Command to update an existing group
/// </summary>
public class UpdateGroupCommand : IRequest<UpdateGroupResponse>
{
    /// <summary>
    /// Group ID from route parameter
    /// </summary>
    [JsonIgnore]
    public Guid GroupId { get; set; }
    
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    /// <summary>
    /// Maximum number of members (null means unlimited, must be greater than 0 if specified)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "MaxMembers must be greater than 0")]
    public int? MaxMembers { get; set; }
    
    public bool IsLocked { get; set; }
}
