using ClassroomService.Domain.DTOs;
using MediatR;

namespace ClassroomService.Application.Features.Reports.Queries;

/// <summary>
/// Query to get detailed information about a specific version
/// </summary>
public class GetVersionDetailQuery : IRequest<ReportHistoryDto>
{
    public Guid ReportId { get; set; }
    public int Version { get; set; }
}
