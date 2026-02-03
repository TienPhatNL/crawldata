using ClassroomService.Domain.Common;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Domain.Entities;

/// <summary>
/// Represents a student's membership in a group
/// Many-to-many relationship between CourseEnrollments and Groups
/// </summary>
public class GroupMember : BaseAuditableEntity
{
    /// <summary>
    /// The group this member belongs to
    /// </summary>
    public Guid GroupId { get; set; }
    
    /// <summary>
    /// The enrollment ID (references CourseEnrollment in this service)
    /// This enforces that only enrolled students can be group members
    /// </summary>
    public Guid EnrollmentId { get; set; }
    
    /// <summary>
    /// Whether this member is the group leader
    /// </summary>
    public bool IsLeader { get; set; } = false;
    
    /// <summary>
    /// The role of this member in the group
    /// </summary>
    public GroupMemberRole Role { get; set; } = GroupMemberRole.Member;
    
    /// <summary>
    /// When the student joined the group
    /// </summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Additional notes about the member's role or responsibilities
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    /// <summary>
    /// The group this member belongs to
    /// </summary>
    public virtual Group Group { get; set; } = null!;
    
    /// <summary>
    /// The course enrollment this membership is based on
    /// </summary>
    public virtual CourseEnrollment Enrollment { get; set; } = null!;

    /// <summary>
    /// Convenience property to get the StudentId from the enrollment
    /// </summary>
    public Guid StudentId => Enrollment?.StudentId ?? Guid.Empty;
}
