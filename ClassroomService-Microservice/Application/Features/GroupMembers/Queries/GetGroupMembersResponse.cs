using ClassroomService.Domain.DTOs;

namespace ClassroomService.Application.Features.GroupMembers.Queries;

public class GetGroupMembersResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<GroupMemberDto> Members { get; set; } = new();
}
