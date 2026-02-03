namespace ClassroomService.Application.Features.CourseCodes.DTOs;

/// <summary>
/// Summary information about a CourseCode (used in lists and selections)
/// </summary>
public class CourseCodeSummaryDto
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
    /// The academic department this course belongs to
    /// </summary>
    public string Department { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether this course code is currently active
    /// </summary>
    public bool IsActive { get; set; }
    
    /// <summary>
    /// Display name combining code and title
    /// </summary>
    public string DisplayName => $"{Code} - {Title}";
}