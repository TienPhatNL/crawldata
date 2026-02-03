using ClassroomService.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace ClassroomService.Domain.Entities;

/// <summary>
/// Represents a topic/category for organizing assignments
/// </summary>
public class Topic : BaseAuditableEntity
{
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Indicates if this topic is active and available for use
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    /// <summary>
    /// Assignments belonging to this topic
    /// </summary>
    public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    
    /// <summary>
    /// Weight configurations for this topic
    /// </summary>
    public virtual ICollection<TopicWeight> TopicWeights { get; set; } = new List<TopicWeight>();
}
