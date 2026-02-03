using WebCrawlerService.Application.Services.Crawl4AI.Models;

namespace WebCrawlerService.Application.Services.Crawl4AI;

/// <summary>
/// Service for analyzing user prompts and extracting intent
/// </summary>
public interface IPromptAnalyzerService
{
    /// <summary>
    /// Analyze a natural language prompt to understand user intent
    /// </summary>
    /// <param name="prompt">User's natural language request</param>
    /// <param name="url">Optional target URL for context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Analysis result with intent, entities, and navigation requirements</returns>
    Task<PromptAnalysisResult> AnalyzePromptAsync(
        string prompt,
        string? url = null,
        CancellationToken cancellationToken = default
    );
}
