using ClassroomService.Domain.DTOs;

namespace ClassroomService.Application.Features.Groups.Commands;

/// <summary>
/// Response for creating a group
/// </summary>
public class CreateGroupResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? GroupId { get; set; }
    public GroupDto? Group { get; set; }
}
