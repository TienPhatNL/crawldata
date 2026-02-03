using MediatR;

namespace ClassroomService.Application.Features.Reports.Commands;

public class GradeReportCommand : IRequest<GradeReportResponse>
{
    public Guid ReportId { get; set; }
    public decimal Grade { get; set; }
    public string? Feedback { get; set; }
}

public class GradeReportResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the user who graded the report
    /// </summary>
    public Guid? ContributorId { get; set; }
    
    /// <summary>
    /// Full name of the user who graded the report
    /// </summary>
    public string? ContributorName { get; set; }
    
    /// <summary>
    /// Role of the user who graded the report
    /// </summary>
    public string? ContributorRole { get; set; }
}
