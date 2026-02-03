using MediatR;
using System.Text.Json.Serialization;

namespace ClassroomService.Application.Features.Assignments.Commands;

public class UpdateAssignmentCommand : IRequest<UpdateAssignmentResponse>
{
    [JsonIgnore]
    public Guid AssignmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime DueDate { get; set; }
    public string Format { get; set; } = string.Empty;
    public int? MaxPoints { get; set; }
}
