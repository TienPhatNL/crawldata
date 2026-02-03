using ClassroomService.Domain.DTOs;

namespace ClassroomService.Application.Features.Assignments.Commands;

public class AssignGroupsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int AssignedCount { get; set; }
    public List<GroupDto> Groups { get; set; } = new();
}
