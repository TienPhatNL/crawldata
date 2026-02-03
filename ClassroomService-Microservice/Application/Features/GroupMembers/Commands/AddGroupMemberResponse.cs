using ClassroomService.Domain.DTOs;

namespace ClassroomService.Application.Features.GroupMembers.Commands;

public class AddGroupMemberResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? MemberId { get; set; }
    public GroupMemberDto? Member { get; set; }
}
