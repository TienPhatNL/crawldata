using ClassroomService.Domain.DTOs;
using MediatR;

namespace ClassroomService.Application.Features.Reports.Queries;

/// <summary>
/// Query to get human-readable timeline of report changes
/// </summary>
public class GetReportTimelineQuery : IRequest<TimelineResponse>
{
    public Guid ReportId { get; set; }
}
