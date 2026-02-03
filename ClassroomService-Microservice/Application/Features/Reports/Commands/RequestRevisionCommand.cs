using MediatR;

namespace ClassroomService.Application.Features.Reports.Commands;

public class RequestRevisionCommand : IRequest<RequestRevisionResponse>
{
    public Guid ReportId { get; set; }
    public string Feedback { get; set; } = string.Empty;
}

public class RequestRevisionResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the user who requested revision
    /// </summary>
    public Guid? ContributorId { get; set; }
    
    /// <summary>
    /// Full name of the user who requested revision
    /// </summary>
    public string? ContributorName { get; set; }
    
    /// <summary>
    /// Role of the user who requested revision
    /// </summary>
    public string? ContributorRole { get; set; }
}
