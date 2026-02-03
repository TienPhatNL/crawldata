using ClassroomService.Domain.Enums;

namespace ClassroomService.Domain.DTOs;

/// <summary>
/// Data transfer object for GroupMember entity
/// </summary>
public class GroupMemberDto
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public Guid EnrollmentId { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public bool IsLeader { get; set; }
    public GroupMemberRole Role { get; set; }
    public string RoleDisplay { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public string? Notes { get; set; }
}
