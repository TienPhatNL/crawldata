using ClassroomService.Domain.DTOs;

namespace ClassroomService.Application.Features.Reports.Commands;

/// <summary>
/// Response for AI content detection check
/// </summary>
public class CheckReportAIResponse
{
    /// <summary>
    /// Whether the check was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Message describing the result
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// AI check result data (null if unsuccessful)
    /// </summary>
    public AICheckResultDto? Result { get; set; }
}
