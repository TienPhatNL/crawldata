using ClassroomService.Domain.Common;
using ClassroomService.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassroomService.Domain.Entities;

public class Assignment : BaseAuditableEntity
{
    public Guid CourseId { get; set; }
    public Guid TopicId { get; set; }
    
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [MaxLength(10000)]
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// When assignment becomes available (optional, null means immediately available)
    /// </summary>
    public DateTime? StartDate { get; set; }
    
    public DateTime DueDate { get; set; }
    public DateTime? ExtendedDueDate { get; set; }
    
    [MaxLength(100)]
    public string Format { get; set; } = string.Empty;
    
    /// <summary>
    /// Assignment status (Draft, Active, Extended, Overdue, Closed)
    /// </summary>
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Draft;
    
    /// <summary>
    /// Indicates if this is a group assignment
    /// </summary>
    public bool IsGroupAssignment { get; set; } = false;
    
    /// <summary>
    /// Maximum points/grade for this assignment
    /// </summary>
    public int? MaxPoints { get; set; }
    
    /// <summary>
    /// Snapshot of the topic weight percentage at the time this assignment was created.
    /// This preserves historical accuracy for grade calculations even if the TopicWeight is updated later.
    /// Nullable for backward compatibility with existing assignments (will use current TopicWeight if null).
    /// </summary>
    public decimal? WeightPercentageSnapshot { get; set; }
    
    /// <summary>
    /// JSON metadata for multiple file attachments (instructions, reference materials, etc.)
    /// Stored as JSON string containing AttachmentMetadata objects
    /// </summary>
    [MaxLength(int.MaxValue)]
    public string? Attachments { get; set; }

    // Navigation properties
    public virtual Course Course { get; set; } = null!;
    public virtual Topic Topic { get; set; } = null!;
    
    /// <summary>
    /// Groups assigned to this assignment (one assignment can be assigned to one group via Group.AssignmentId)
    /// </summary>
    public virtual ICollection<Group> AssignedGroups { get; set; } = new List<Group>();
    
    /// <summary>
    /// Reports/submissions for this assignment
    /// </summary>
    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
}