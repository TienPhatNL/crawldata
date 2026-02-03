namespace ClassroomService.Application.Features.GroupMembers.Commands;

/// <summary>
/// Response for assigning a group leader
/// </summary>
public class AssignGroupLeaderResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? GroupId { get; set; }
    public Guid? NewLeaderId { get; set; }
    public string? NewLeaderName { get; set; }
    public Guid? PreviousLeaderId { get; set; }
    public string? PreviousLeaderName { get; set; }
}
