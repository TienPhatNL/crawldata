using ClassroomService.Domain.Common;

namespace ClassroomService.Domain.Entities;

/// <summary>
/// Represents a group within a course where students collaborate
/// </summary>
public class Group : BaseAuditableEntity
{
    /// <summary>
    /// The course this group belongs to
    /// </summary>
    public Guid CourseId { get; set; }
    
    /// <summary>
    /// The name of the group
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of the group's purpose or project
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Maximum number of members allowed (null means unlimited)
    /// </summary>
    public int? MaxMembers { get; set; }
    
    /// <summary>
    /// Whether the group is locked (prevents adding/removing members)
    /// </summary>
    public bool IsLocked { get; set; } = false;
    
    /// <summary>
    /// Optional assignment assigned to this group
    /// </summary>
    public Guid? AssignmentId { get; set; }

    // Navigation properties
    /// <summary>
    /// The course this group belongs to
    /// </summary>
    public virtual Course Course { get; set; } = null!;
    
    /// <summary>
    /// Students who are members of this group
    /// </summary>
    public virtual ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
    
    /// <summary>
    /// Assignment assigned to this group (if any)
    /// </summary>
    public virtual Assignment? Assignment { get; set; }
    
    /// <summary>
    /// Reports/submissions made by this group
    /// </summary>
    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
}