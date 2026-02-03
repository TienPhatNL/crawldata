using ClassroomService.Domain.DTOs;
using MediatR;

namespace ClassroomService.Application.Features.Reports.Queries;

/// <summary>
/// Query to get complete history of a report with pagination
/// </summary>
public class GetReportHistoryQuery : IRequest<ReportHistoryResponse>
{
    public Guid ReportId { get; set; }
    
    /// <summary>
    /// Page number (1-based, default: 1)
    /// </summary>
    public int PageNumber { get; set; } = 1;
    
    /// <summary>
    /// Items per page (default: 20, max: 100)
    /// </summary>
    public int PageSize { get; set; } = 20;
}
