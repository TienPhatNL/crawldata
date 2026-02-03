namespace ClassroomService.Application.Features.CourseCodes.DTOs;

/// <summary>
/// Data transfer object for CourseCode
/// </summary>
public class CourseCodeDto
{
    /// <summary>
    /// Unique identifier for the course code
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// The course code (e.g., "CS101", "MATH202")
    /// </summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// The title/name of the course
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of the course content and objectives
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Whether this course code is currently active
    /// </summary>
    public bool IsActive { get; set; }
    
    /// <summary>
    /// The academic department this course belongs to
    /// </summary>
    public string Department { get; set; } = string.Empty;
    
    /// <summary>
    /// When the course code was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// When the course code was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Number of active course sections using this course code
    /// </summary>
    public int ActiveCoursesCount { get; set; }
    
    /// <summary>
    /// Total number of course sections (including inactive) using this course code
    /// </summary>
    public int TotalCoursesCount { get; set; }
}