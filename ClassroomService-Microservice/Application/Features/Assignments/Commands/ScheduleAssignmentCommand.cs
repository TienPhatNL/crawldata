using MediatR;
using System.Text.Json.Serialization;

namespace ClassroomService.Application.Features.Assignments.Commands;

/// <summary>
/// Command to schedule or unschedule an assignment (Draft - Scheduled)
/// </summary>

public class ScheduleAssignmentCommand : IRequest<ScheduleAssignmentResponse>
{
    /// <summary>
    /// Assignment ID to schedule/unschedule
    /// </summary>
    [JsonIgnore]
    public Guid AssignmentId { get; set; }

    /// <summary>
    /// Whether to schedule (true) or unschedule back to draft (false)
    /// </summary>
    public bool Schedule { get; set; } = true;
}

/// <summary>
/// Response for schedule assignment command
/// </summary>
public class ScheduleAssignmentResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DTOs.AssignmentDetailDto? Assignment { get; set; }
}
