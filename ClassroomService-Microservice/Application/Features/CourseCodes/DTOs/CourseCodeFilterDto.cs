using System.ComponentModel.DataAnnotations;

namespace ClassroomService.Application.Features.CourseCodes.DTOs;

/// <summary>
/// Filter and pagination options for CourseCode queries
/// </summary>
public class CourseCodeFilterDto
{
    /// <summary>
    /// Search by course code (partial match)
    /// </summary>
    public string? Code { get; set; }
    
    /// <summary>
    /// Search by course title (partial match)
    /// </summary>
    public string? Title { get; set; }
    
    /// <summary>
    /// Filter by department
    /// </summary>
    public string? Department { get; set; }
    
    /// <summary>
    /// Filter by active status
    /// </summary>
    public bool? IsActive { get; set; }
    
    /// <summary>
    /// Filter course codes created after this date
    /// </summary>
    public DateTime? CreatedAfter { get; set; }
    
    /// <summary>
    /// Filter course codes created before this date
    /// </summary>
    public DateTime? CreatedBefore { get; set; }
    
    /// <summary>
    /// Include only course codes that have active courses
    /// </summary>
    public bool? HasActiveCourses { get; set; }
    
    /// <summary>
    /// Page number (1-based)
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;
    
    /// <summary>
    /// Number of items per page
    /// </summary>
    [Range(1, 100)]
    public int PageSize { get; set; } = 10;
    
    /// <summary>
    /// Sort field (Code, Title, Department, CreatedAt, ActiveCoursesCount)
    /// </summary>
    public string? SortBy { get; set; } = "Code";
    
    /// <summary>
    /// Sort direction (asc or desc)
    /// </summary>
    public string SortDirection { get; set; } = "asc";
}