namespace ClassroomService.Application.Features.Courses.Commands;

/// <summary>
/// Response for updating course access code
/// </summary>
public class UpdateCourseAccessCodeResponse
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
    /// The new access code (if generated)
    /// </summary>
    public string? AccessCode { get; set; }

    /// <summary>
    /// When the access code was created
    /// </summary>
    public DateTime? AccessCodeCreatedAt { get; set; }

    /// <summary>
    /// When the access code expires
    /// </summary>
    public DateTime? AccessCodeExpiresAt { get; set; }
}