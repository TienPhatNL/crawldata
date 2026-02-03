using ClassroomService.Application.Features.CourseCodes.DTOs;

namespace ClassroomService.Application.Features.CourseCodes.Commands;

/// <summary>
/// Response for CreateCourseCode command
/// </summary>
public class CreateCourseCodeResponse
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
    /// The ID of the created course code
    /// </summary>
    public Guid? CourseCodeId { get; set; }

    /// <summary>
    /// The created course code details
    /// </summary>
    public CourseCodeDto? CourseCode { get; set; }
}