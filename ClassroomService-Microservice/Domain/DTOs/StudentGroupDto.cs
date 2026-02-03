namespace ClassroomService.Domain.DTOs;

/// <summary>
/// Data transfer object for student's group view
/// </summary>
public class StudentGroupDto
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string? GroupDescription { get; set; }
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public bool IsLeader { get; set; }
    public int MemberCount { get; set; }
    public int? MaxMembers { get; set; }
    public bool IsLocked { get; set; }
    public Guid? AssignmentId { get; set; }
    public string? AssignmentTitle { get; set; }
    public DateTime? AssignmentDueDate { get; set; }
    public DateTime JoinedAt { get; set; }
}
