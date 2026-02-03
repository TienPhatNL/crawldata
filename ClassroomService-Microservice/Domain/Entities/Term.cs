using ClassroomService.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace ClassroomService.Domain.Entities;

/// <summary>
/// Represents an academic term (e.g., "Spring", "Fall", "Q1", "Semester 1", etc.)
/// </summary>
public class Term : BaseAuditableEntity
{
    /// <summary>
    /// Term name (e.g., "Spring", "Fall", "Q1", "Semester 1", "Winter Break")
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional description of the term
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Start date of the term
    /// </summary>
    [Required]
    public DateTime StartDate { get; set; }
    
    /// <summary>
    /// End date of the term
    /// </summary>
    [Required]
    public DateTime EndDate { get; set; }
    
    /// <summary>
    /// Whether this term is active and available for selection
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Courses associated with this term
    /// </summary>
    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
}
