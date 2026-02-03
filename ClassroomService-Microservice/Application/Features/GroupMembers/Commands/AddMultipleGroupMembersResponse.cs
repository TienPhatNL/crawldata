using ClassroomService.Domain.DTOs;

namespace ClassroomService.Application.Features.GroupMembers.Commands;

/// <summary>
/// Response for adding multiple members to a group
/// </summary>
public class AddMultipleGroupMembersResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TotalRequested { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<BulkAddResult> Results { get; set; } = new();
}

/// <summary>
/// Result for individual student addition
/// </summary>
public class BulkAddResult
{
    public Guid StudentId { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public GroupMemberDto? Member { get; set; }
}
