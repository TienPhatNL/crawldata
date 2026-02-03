using ClassroomService.Domain.DTOs;

namespace ClassroomService.Application.Features.GroupMembers.Queries;

/// <summary>
/// Response for getting a group member by ID
/// </summary>
public class GetGroupMemberByIdResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public GroupMemberDto? Member { get; set; }
}
