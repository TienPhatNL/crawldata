using ClassroomService.Application.Features.CourseCodes.DTOs;

namespace ClassroomService.Application.Features.CourseCodes.Queries;

/// <summary>
/// Response for GetAllCourseCodes query
/// </summary>
public class GetAllCourseCodesResponse
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Response message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// List of course codes
    /// </summary>
    public List<CourseCodeDto> CourseCodes { get; set; } = new();

    /// <summary>
    /// Total number of course codes matching the filter
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int CurrentPage { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage { get; set; }

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage { get; set; }
}