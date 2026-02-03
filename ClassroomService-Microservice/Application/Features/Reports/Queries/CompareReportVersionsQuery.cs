using ClassroomService.Domain.DTOs;
using MediatR;

namespace ClassroomService.Application.Features.Reports.Queries;

/// <summary>
/// Query to compare aggregate versions (all sequences within each version)
/// Example: Compare all changes in version 1 vs all changes in version 4
/// </summary>
public class CompareReportVersionsQuery : IRequest<CompareVersionsResponse>
{
    public Guid ReportId { get; set; }
    
    /// <summary>
    /// First version number to compare
    /// </summary>
    public int Version1 { get; set; }
    
    /// <summary>
    /// Second version number to compare
    /// </summary>
    public int Version2 { get; set; }
}
