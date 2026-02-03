namespace ClassroomService.Domain.Interfaces;

/// <summary>
/// Service for detecting AI-generated content in text
/// </summary>
public interface IAIDetectionService
{
    /// <summary>
    /// Analyzes text content to determine likelihood of AI generation
    /// </summary>
    /// <param name="content">The text content to analyze</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>
    /// Tuple containing:
    /// - Success: Whether the check completed successfully
    /// - AIPercentage: Percentage likelihood of AI generation (0-100)
    /// - ErrorMessage: Error message if unsuccessful
    /// - RawResponse: Raw JSON response from the AI detection service
    /// </returns>
    Task<(bool Success, decimal? AIPercentage, string? ErrorMessage, string? RawResponse)> 
        CheckContentAsync(string content, CancellationToken cancellationToken = default);
}
