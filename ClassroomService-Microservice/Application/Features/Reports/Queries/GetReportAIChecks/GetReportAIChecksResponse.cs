using ClassroomService.Domain.DTOs;

namespace ClassroomService.Application.Features.Reports.Queries;

/// <summary>
/// Response containing AI check history for a report
/// </summary>
public class GetReportAIChecksResponse
{
    /// <summary>
    /// Whether the query was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Message describing the result
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// List of AI checks for the report
    /// </summary>
    public List<AICheckResultDto> Checks { get; set; } = new();
}
