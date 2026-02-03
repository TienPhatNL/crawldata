using MediatR;
using System.ComponentModel.DataAnnotations;

namespace ClassroomService.Application.Features.Groups.Commands;

/// <summary>
/// Command to create a new group in a course
/// </summary>
public class CreateGroupCommand : IRequest<CreateGroupResponse>
{
    /// <summary>
    /// The course ID to create the group in
    /// </summary>
    public Guid CourseId { get; set; }
    
    /// <summary>
    /// The name of the group
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional description of the group
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Maximum number of members (null means unlimited, must be greater than 0 if specified)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "MaxMembers must be greater than 0")]
    public int? MaxMembers { get; set; }
}
