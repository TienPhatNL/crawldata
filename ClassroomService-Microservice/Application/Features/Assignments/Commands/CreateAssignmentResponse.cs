using ClassroomService.Application.Features.Assignments.DTOs;

namespace ClassroomService.Application.Features.Assignments.Commands;

public class CreateAssignmentResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public Guid? AssignmentId { get; set; }
    public AssignmentDetailDto? Assignment { get; set; }
    public int GroupsAssigned { get; set; }
}