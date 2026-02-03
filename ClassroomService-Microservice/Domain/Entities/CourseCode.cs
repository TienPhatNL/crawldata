using ClassroomService.Domain.Common;

namespace ClassroomService.Domain.Entities;

/// <summary>
/// Represents a course code/curriculum item that can have multiple course sections
/// </summary>
public class CourseCode : BaseAuditableEntity
{
    /// <summary>
    /// The course code identifier (e.g., "CS101", "MATH202")
    /// </summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// The title/name of the course (e.g., "Introduction to Computer Science")
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional description of the course content and objectives
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Whether this course code is currently active and available for use
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// The academic department this course code belongs to
    /// </summary>
    public string Department { get; set; } = string.Empty;

    // Navigation properties
    /// <summary>
    /// All course sections that use this course code
    /// </summary>
    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
    
    /// <summary>
    /// Topic weight configurations for this course code
    /// </summary>
    public virtual ICollection<TopicWeight> TopicWeights { get; set; } = new List<TopicWeight>();
}