namespace ClassroomService.Application.Features.Courses.Queries;

/// <summary>
/// Response for getting course access code
/// </summary>
public class GetCourseAccessCodeResponse
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
    /// Whether the course requires an access code
    /// </summary>
    public bool RequiresAccessCode { get; set; }

    /// <summary>
    /// The access code
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

    /// <summary>
    /// Whether the access code is expired
    /// </summary>
    public bool IsExpired { get; set; }

    /// <summary>
    /// Number of failed attempts
    /// </summary>
    public int FailedAttempts { get; set; }
}