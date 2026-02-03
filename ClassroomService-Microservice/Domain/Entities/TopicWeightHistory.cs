using ClassroomService.Domain.Common;
using ClassroomService.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassroomService.Domain.Entities;

/// <summary>
/// Tracks all changes made to TopicWeight configurations for audit trail
/// </summary>
public class TopicWeightHistory : BaseAuditableEntity
{
    /// <summary>
    /// Reference to the TopicWeight configuration
    /// </summary>
    public Guid TopicWeightId { get; set; }
    
    /// <summary>
    /// The topic this weight applies to
    /// </summary>
    public Guid TopicId { get; set; }
    
    /// <summary>
    /// Course code this weight applies to (null if course-specific)
    /// </summary>
    public Guid? CourseCodeId { get; set; }
    
    /// <summary>
    /// Specific course this weight applies to (null if code-level)
    /// </summary>
    public Guid? SpecificCourseId { get; set; }
    
    /// <summary>
    /// Term ID affected by this change (if applicable)
    /// </summary>
    public Guid? TermId { get; set; }
    
    /// <summary>
    /// Term name affected by this change (denormalized for easy display)
    /// </summary>
    [MaxLength(100)]
    public string? TermName { get; set; }
    
    /// <summary>
    /// OLD weight percentage before the change (null for creation)
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? OldWeightPercentage { get; set; }
    
    /// <summary>
    /// NEW weight percentage after the change
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal NewWeightPercentage { get; set; }
    
    /// <summary>
    /// User who made the change
    /// </summary>
    public Guid ModifiedBy { get; set; }
    
    /// <summary>
    /// When the change was made
    /// </summary>
    public DateTime ModifiedAt { get; set; }
    
    /// <summary>
    /// Type of change (Created, Updated, Deleted)
    /// </summary>
    public WeightHistoryAction Action { get; set; }
    
    /// <summary>
    /// Optional reason for the change
    /// </summary>
    [MaxLength(500)]
    public string? ChangeReason { get; set; }
    
    /// <summary>
    /// Comma-separated list of all affected term names (for CourseCode-level changes)
    /// </summary>
    [MaxLength(200)]
    public string? AffectedTerms { get; set; }
    
    // Navigation properties
    public virtual TopicWeight TopicWeight { get; set; } = null!;
    public virtual Topic Topic { get; set; } = null!;
    public virtual CourseCode? CourseCode { get; set; }
    public virtual Course? SpecificCourse { get; set; }
    public virtual Term? Term { get; set; }
}
