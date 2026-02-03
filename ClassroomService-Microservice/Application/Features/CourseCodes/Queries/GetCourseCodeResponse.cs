using ClassroomService.Application.Features.CourseCodes.DTOs;

namespace ClassroomService.Application.Features.CourseCodes.Queries;

/// <summary>
/// Response for GetCourseCode query
/// </summary>
public class GetCourseCodeResponse
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
    /// The course code details
    /// </summary>
    public CourseCodeDto? CourseCode { get; set; }
}