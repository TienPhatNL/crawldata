using ClassroomService.Application.Features.Assignments.DTOs;

namespace ClassroomService.Application.Features.Assignments.Queries;

public class GetGroupAssignmentResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public AssignmentDetailDto? Assignment { get; set; }
    public bool HasAssignment { get; set; }
}
