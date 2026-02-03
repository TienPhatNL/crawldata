using MediatR;
using System.Text.Json.Serialization;

namespace ClassroomService.Application.Features.Assignments.Commands;

public class ExtendDueDateCommand : IRequest<ExtendDueDateResponse>
{
    [JsonIgnore]
    public Guid AssignmentId { get; set; }
    public DateTime ExtendedDueDate { get; set; }
}
