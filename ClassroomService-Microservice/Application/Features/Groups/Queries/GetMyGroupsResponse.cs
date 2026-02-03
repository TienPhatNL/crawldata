using ClassroomService.Domain.DTOs;

namespace ClassroomService.Application.Features.Groups.Queries;

/// <summary>
/// Response for getting groups the student belongs to
/// </summary>
public class GetMyGroupsResponse
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Response message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// List of groups the student is a member of
    /// </summary>
    public List<StudentGroupMembershipDto> Groups { get; set; } = new List<StudentGroupMembershipDto>();

    /// <summary>
    /// Total number of groups
    /// </summary>
    public int TotalGroups { get; set; }
}

/// <summary>
/// DTO representing a student's group membership
/// </summary>
public class StudentGroupMembershipDto
{
    /// <summary>
    /// Group ID
    /// </summary>
    public Guid GroupId { get; set; }

    /// <summary>
    /// Group name
    /// </summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// Group description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Course ID
    /// </summary>
    public Guid CourseId { get; set; }

    /// <summary>
    /// Course name
    /// </summary>
    public string CourseName { get; set; } = string.Empty;

    /// <summary>
    /// Course code
    /// </summary>
    public string CourseCode { get; set; } = string.Empty;

    /// <summary>
    /// Whether the group is locked
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// Maximum members allowed (null if unlimited)
    /// </summary>
    public int? MaxMembers { get; set; }

    /// <summary>
    /// Current number of members
    /// </summary>
    public int MemberCount { get; set; }

    /// <summary>
    /// Assignment ID (if assigned)
    /// </summary>
    public Guid? AssignmentId { get; set; }

    /// <summary>
    /// Assignment title (if assigned)
    /// </summary>
    public string? AssignmentTitle { get; set; }

    /// <summary>
    /// Whether the student is a leader of this group
    /// </summary>
    public bool IsLeader { get; set; }

    /// <summary>
    /// Student's role in the group
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// When the student joined the group
    /// </summary>
    public DateTime JoinedAt { get; set; }

    /// <summary>
    /// Group creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
