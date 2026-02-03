namespace ClassroomService.Application.Features.Assignments.Commands;

public class DeleteAssignmentResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int GroupsUnassigned { get; set; }
}
