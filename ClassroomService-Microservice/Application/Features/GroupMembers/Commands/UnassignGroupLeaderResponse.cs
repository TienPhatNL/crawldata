namespace ClassroomService.Application.Features.GroupMembers.Commands;

public class UnassignGroupLeaderResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? GroupId { get; set; }
    public Guid? PreviousLeaderId { get; set; }
    public string? PreviousLeaderName { get; set; }
}
