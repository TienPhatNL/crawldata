using ClassroomService.Domain.DTOs;

namespace ClassroomService.Application.Features.Groups.Queries;

public class GetCourseGroupsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<GroupDto> Groups { get; set; } = new();
}
