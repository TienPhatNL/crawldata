namespace ClassroomService.Application.Features.GroupMembers.Commands;

public class RemoveGroupMemberResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
