using ClassroomService.Domain.DTOs;

namespace ClassroomService.Application.Features.Groups.Commands;

/// <summary>
/// Response for randomizing students into groups
/// </summary>
public class RandomizeStudentsToGroupsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? CourseId { get; set; }
    public int GroupsCreated { get; set; }
    public int StudentsAssigned { get; set; }
    public List<RandomizedGroupInfo> Groups { get; set; } = new();
}

/// <summary>
/// Information about a randomized group
/// </summary>
public class RandomizedGroupInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public Guid? LeaderId { get; set; }
    public string? LeaderName { get; set; }
}
