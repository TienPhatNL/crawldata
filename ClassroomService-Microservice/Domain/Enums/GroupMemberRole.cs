namespace ClassroomService.Domain.Enums;

/// <summary>
/// Represents the role of a member within a group
/// </summary>
public enum GroupMemberRole
{
    /// <summary>
    /// Regular member with no special privileges
    /// </summary>
    Member = 1,
    
    /// <summary>
    /// Group leader with coordination responsibilities
    /// </summary>
    Leader = 2
}
