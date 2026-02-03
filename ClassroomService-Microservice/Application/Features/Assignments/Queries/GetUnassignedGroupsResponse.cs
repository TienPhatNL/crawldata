using ClassroomService.Domain.DTOs;

namespace ClassroomService.Application.Features.Assignments.Queries;

public class GetUnassignedGroupsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public List<GroupDto> UnassignedGroups { get; set; } = new();
    public int TotalGroups { get; set; }
    public int AssignedGroupsCount { get; set; }
    public int UnassignedGroupsCount { get; set; }
}
