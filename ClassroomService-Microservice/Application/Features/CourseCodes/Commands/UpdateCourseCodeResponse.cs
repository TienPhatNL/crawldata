using ClassroomService.Application.Features.CourseCodes.DTOs;

namespace ClassroomService.Application.Features.CourseCodes.Commands;

/// <summary>
/// Response for UpdateCourseCode command
/// </summary>
public class UpdateCourseCodeResponse
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
    /// The updated course code details
    /// </summary>
    public CourseCodeDto? CourseCode { get; set; }
}