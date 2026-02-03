using ClassroomService.Domain.DTOs;

namespace ClassroomService.Application.Features.Groups.Commands;

public class UpdateGroupResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public GroupDto? Group { get; set; }
}
