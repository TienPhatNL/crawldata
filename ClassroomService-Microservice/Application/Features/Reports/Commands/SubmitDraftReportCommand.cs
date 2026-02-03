using MediatR;

namespace ClassroomService.Application.Features.Reports.Commands;

/// <summary>
/// Command to submit a draft report for review (Draft → UnderReview)
/// </summary>
public class SubmitDraftReportCommand : IRequest<SubmitDraftReportResponse>
{
    public Guid ReportId { get; set; }
}

public class SubmitDraftReportResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
