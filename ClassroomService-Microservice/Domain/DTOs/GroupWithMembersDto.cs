namespace ClassroomService.Domain.DTOs;

/// <summary>
/// Data transfer object for Group with its members (detailed view)
/// </summary>
public class GroupWithMembersDto
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? MaxMembers { get; set; }
    public bool IsLocked { get; set; }
    public Guid? AssignmentId { get; set; }
    public string? AssignmentTitle { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public List<GroupMemberDto> Members { get; set; } = new();
}
