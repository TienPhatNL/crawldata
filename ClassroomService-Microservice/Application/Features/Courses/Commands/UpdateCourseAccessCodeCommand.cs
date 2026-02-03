using MediatR;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Application.Features.Courses.Commands;

/// <summary>
/// Command to update course access code
/// </summary>
public class UpdateCourseAccessCodeCommand : IRequest<UpdateCourseAccessCodeResponse>
{
    /// <summary>
    /// Course ID
    /// </summary>
    public Guid CourseId { get; set; }

    /// <summary>
    /// Whether the course requires an access code
    /// </summary>
    public bool RequiresAccessCode { get; set; }

    /// <summary>
    /// Type of access code to generate
    /// </summary>
    public AccessCodeType? AccessCodeType { get; set; }

    /// <summary>
    /// Custom access code (only used if AccessCodeType is Custom)
    /// </summary>
    public string? CustomAccessCode { get; set; }

    /// <summary>
    /// When the access code expires
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Whether to regenerate the current code
    /// </summary>
    public bool RegenerateCode { get; set; } = false;

    /// <summary>
    /// Lecturer ID (set from current user context)
    /// </summary>
    public Guid LecturerId { get; set; }
}