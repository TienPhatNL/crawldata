namespace ClassroomService.Application.Features.Assignments.Commands;

public class UnassignGroupsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int UnassignedCount { get; set; }
}
