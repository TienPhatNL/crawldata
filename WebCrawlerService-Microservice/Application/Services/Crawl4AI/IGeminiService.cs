namespace WebCrawlerService.Application.Services.Crawl4AI;

/// <summary>
/// Service for interacting with Google Gemini LLM
/// </summary>
public interface IGeminiService
{
    /// <summary>
    /// Generate content using Gemini
    /// </summary>
    /// <param name="prompt">Input prompt</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated text response</returns>
    Task<string> GenerateContentAsync(
        string prompt,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Generate structured JSON response from Gemini
    /// </summary>
    /// <typeparam name="T">Expected response type</typeparam>
    /// <param name="prompt">Input prompt</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Parsed JSON response</returns>
    Task<T?> GenerateJsonAsync<T>(
        string prompt,
        CancellationToken cancellationToken = default
    ) where T : class;
}
