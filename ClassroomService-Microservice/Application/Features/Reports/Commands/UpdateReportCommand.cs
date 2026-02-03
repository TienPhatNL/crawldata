using System.Text.Json.Serialization;
using MediatR;

namespace ClassroomService.Application.Features.Reports.Commands;

public class UpdateReportCommand : IRequest<UpdateReportResponse>
{
    [JsonIgnore]
    public Guid ReportId { get; set; }
    public string Submission { get; set; } = string.Empty;
}

public class UpdateReportResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
