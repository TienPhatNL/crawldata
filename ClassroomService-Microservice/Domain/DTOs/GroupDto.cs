using ClassroomService.Domain.Enums;
using System.Text.Json.Serialization;

namespace ClassroomService.Domain.DTOs;

/// <summary>
/// Data transfer object for Group entity
/// </summary>
public class GroupDto
{
    //[JsonIgnore]
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? MaxMembers { get; set; }
    public bool IsLocked { get; set; }
    public Guid? AssignmentId { get; set; }
    public string? AssignmentTitle { get; set; }
    public int MemberCount { get; set; }
    public string? LeaderName { get; set; }
    public Guid? LeaderId { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
}
