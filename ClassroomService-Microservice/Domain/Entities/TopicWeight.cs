using ClassroomService.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace ClassroomService.Domain.Entities;

/// <summary>
/// Represents weight configuration for topics per course code or specific course
/// </summary>
public class TopicWeight : BaseAuditableEntity
{
    /// <summary>
    /// Reference to the topic
    /// </summary>
    public Guid TopicId { get; set; }
    
    /// <summary>
    /// Weight applies to all courses with this course code (null if SpecificCourseId is set)
    /// </summary>
    public Guid? CourseCodeId { get; set; }
    
    /// <summary>
    /// Weight override for a specific course instance (null if CourseCodeId is set)
    /// </summary>
    public Guid? SpecificCourseId { get; set; }
    
    /// <summary>
    /// Weight percentage (e.g., 40.0 for 40%)
    /// </summary>
    [Required]
    public decimal WeightPercentage { get; set; }
    
    /// <summary>
    /// Optional description for this weight configuration
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }
    
    /// <summary>
    /// User who configured this weight
    /// </summary>
    public Guid ConfiguredBy { get; set; }
    
    /// <summary>
    /// Soft delete flag - if true, this weight configuration is deleted but preserved for history
    /// </summary>
    public bool IsDeleted { get; set; }
    
    /// <summary>
    /// When this weight was soft-deleted (null if not deleted)
    /// </summary>
    public DateTime? DeletedAt { get; set; }
    
    /// <summary>
    /// User who deleted this weight (null if not deleted)
    /// </summary>
    public Guid? DeletedBy { get; set; }
    
    // Navigation properties
    public virtual Topic Topic { get; set; } = null!;
    public virtual CourseCode? CourseCode { get; set; }
    public virtual Course? SpecificCourse { get; set; }
}
