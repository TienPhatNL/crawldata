using ClassroomService.Domain.DTOs;

namespace ClassroomService.Application.Features.Groups.Queries;

/// <summary>
/// Response for getting a group by ID
/// </summary>
public class GetGroupByIdResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public GroupDto? Group { get; set; }
}
