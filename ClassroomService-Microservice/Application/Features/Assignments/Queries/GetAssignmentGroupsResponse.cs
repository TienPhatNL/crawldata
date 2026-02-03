using ClassroomService.Domain.DTOs;

namespace ClassroomService.Application.Features.Assignments.Queries;

public class GetAssignmentGroupsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid AssignmentId { get; set; }
    public string AssignmentTitle { get; set; } = string.Empty;
    public List<GroupDto> Groups { get; set; } = new();
    public int TotalGroups { get; set; }
}
