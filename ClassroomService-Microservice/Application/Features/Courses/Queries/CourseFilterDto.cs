using System.ComponentModel.DataAnnotations;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Application.Features.Courses.Queries;

/// <summary>
/// Filter object for searching courses
/// </summary>
public class CourseFilterDto
{
    /// <summary>
    /// Search by course name (partial match)
    /// </summary>
    /// <example>Computer Science</example>
    public string? Name { get; set; }

    /// <summary>
    /// Search by course code (partial match)
    /// </summary>
    /// <example>CS</example>
    public string? CourseCode { get; set; }

    /// <summary>
    /// Filter by lecturer name (partial match)
    /// </summary>
    /// <example>John Smith</example>
    public string? LecturerName { get; set; }

    /// <summary>
    /// Filter by course status (Active, PendingApproval, Rejected)
    /// </summary>
    /// <example>Active</example>
    public CourseStatus? Status { get; set; }

    /// <summary>
    /// Filter courses created after this date
    /// </summary>
    /// <example>2024-01-01T00:00:00Z</example>
    public DateTime? CreatedAfter { get; set; }

    /// <summary>
    /// Filter courses created before this date
    /// </summary>
    /// <example>2024-12-31T23:59:59Z</example>
    public DateTime? CreatedBefore { get; set; }

    /// <summary>
    /// Minimum number of enrolled students
    /// </summary>
    /// <example>1</example>
    [Range(0, int.MaxValue, ErrorMessage = "Minimum enrollment count must be 0 or greater")]
    public int? MinEnrollmentCount { get; set; }

    /// <summary>
    /// Maximum number of enrolled students
    /// </summary>
    /// <example>50</example>
    [Range(0, int.MaxValue, ErrorMessage = "Maximum enrollment count must be 0 or greater")]
    public int? MaxEnrollmentCount { get; set; }

    /// <summary>
    /// Page number for pagination (1-based)
    /// </summary>
    /// <example>1</example>
    [Range(1, int.MaxValue, ErrorMessage = "Page number must be 1 or greater")]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page (max 100)
    /// </summary>
    /// <example>10</example>
    [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Sort field (Name, CourseCode, CreatedAt, EnrollmentCount)
    /// </summary>
    /// <example>CreatedAt</example>
    public string? SortBy { get; set; } = "CreatedAt";

    /// <summary>
    /// Sort direction (asc or desc)
    /// </summary>
    /// <example>desc</example>
    public string SortDirection { get; set; } = "desc";
}