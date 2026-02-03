using MediatR;
using ClassroomService.Application.Features.Courses.Queries;

namespace ClassroomService.Application.Features.Courses.Queries;

/// <summary>
/// Query to get courses by term and year
/// </summary>
public class GetCoursesByTermAndYearQuery : IRequest<GetCoursesByTermAndYearResponse>
{
    /// <summary>
    /// Term ID to filter courses
    /// </summary>
    public Guid TermId { get; set; }
    
    /// <summary>
    /// Optional: Filter by course status
    /// </summary>
    public ClassroomService.Domain.Enums.CourseStatus? Status { get; set; }
    
    /// <summary>
    /// Optional: Filter by lecturer ID
    /// </summary>
    public Guid? LecturerId { get; set; }
    
    /// <summary>
    /// Optional: Search by course code
    /// </summary>
    public string? CourseCode { get; set; }
    
    /// <summary>
    /// Page number for pagination
    /// </summary>
    public int Page { get; set; } = 1;
    
    /// <summary>
    /// Page size for pagination
    /// </summary>
    public int PageSize { get; set; } = 20;
    
    /// <summary>
    /// Sort field (Name, CourseCode, EnrollmentCount)
    /// </summary>
    public string SortBy { get; set; } = "Name";
    
    /// <summary>
    /// Sort direction (asc or desc)
    /// </summary>
    public string SortDirection { get; set; } = "asc";
    
    /// <summary>
    /// Current user ID (for authorization)
    /// </summary>
    public Guid? CurrentUserId { get; set; }
    
    /// <summary>
    /// Current user role
    /// </summary>
    public string? CurrentUserRole { get; set; }
}

/// <summary>
/// Response for get courses by term and year query
/// </summary>
public class GetCoursesByTermAndYearResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<CourseDto> Courses { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public string TermName { get; set; } = string.Empty;
}
