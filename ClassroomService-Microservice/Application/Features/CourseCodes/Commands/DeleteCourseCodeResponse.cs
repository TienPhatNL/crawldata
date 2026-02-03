namespace ClassroomService.Application.Features.CourseCodes.Commands;

/// <summary>
/// Response for DeleteCourseCode command
/// </summary>
public class DeleteCourseCodeResponse
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Response message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}