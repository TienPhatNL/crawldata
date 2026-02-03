using MediatR;

namespace ClassroomService.Application.Features.Reports.Commands;

public class SubmitReportCommand : IRequest<SubmitReportResponse>
{
    public Guid AssignmentId { get; set; }
    public Guid? GroupId { get; set; }
    public string Submission { get; set; } = string.Empty;
    public bool IsGroupSubmission { get; set; }
}
