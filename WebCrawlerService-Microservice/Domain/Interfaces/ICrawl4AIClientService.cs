using WebCrawlerService.Domain.Models;
using WebCrawlerService.Domain.Models.Crawl4AI;

namespace WebCrawlerService.Domain.Interfaces;

/// <summary>
/// Client service for communicating with crawl4ai agents
/// </summary>
public interface ICrawl4AIClientService
{
    /// <summary>
    /// Submit crawl job for background processing (fire-and-forget)
    /// Returns immediately after job submission. Results delivered via Kafka.
    /// </summary>
    /// <param name="url">Target URL</param>
    /// <param name="prompt">Natural language extraction request</param>
    /// <param name="jobId">Job ID for tracking (required)</param>
    /// <param name="userId">User ID for tracking (required)</param>
    /// <param name="navigationSteps">Optional pre-defined navigation steps</param>
    /// <param name="maxPages">Optional max pages (null = Python handles prompt extraction + default)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Submission result indicating if job was accepted or completed synchronously</returns>
    Task<CrawlSubmissionResult> SubmitCrawlJobAsync(
        string url,
        string prompt,
        string jobId,
        string userId,
        List<NavigationStep>? navigationSteps = null,
        int? maxPages = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Execute intelligent crawl with natural language prompt (synchronous/legacy)
    /// This method waits for crawl completion. Use SubmitCrawlJobAsync for fire-and-forget.
    /// </summary>
    /// <param name="url">Target URL</param>
    /// <param name="prompt">Natural language extraction request</param>
    /// <param name="navigationSteps">Optional pre-defined navigation steps</param>
    /// <param name="jobId">Optional job ID for Kafka progress tracking</param>
    /// <param name="userId">Optional user ID for Kafka progress tracking</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Crawl response with extracted data</returns>
    Task<Crawl4AIResponse> IntelligentCrawlAsync(
        string url,
        string prompt,
        List<NavigationStep>? navigationSteps = null,
        string? jobId = null,
        string? userId = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Generate summary and charts using the Python agent
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <param name="data">Crawled data</param>
    /// <param name="prompt">Optional user prompt to guide summary generation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Summary response</returns>
    Task<SummaryResponse> GenerateSummaryAsync(
        string jobId,
        object data,
        string? prompt = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Analyze page structure and get suggested navigation plan
    /// </summary>
    /// <param name="url">Target URL</param>
    /// <param name="prompt">User's intent</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of suggested navigation steps</returns>
    Task<List<Dictionary<string, object>>> AnalyzePageAsync(
        string url,
        string prompt,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Check if crawl4ai agent is healthy
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if healthy</returns>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ask a question based on provided context (RAG)
    /// </summary>
    /// <param name="context">Context data (JSON/Text)</param>
    /// <param name="question">User's question</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Natural language answer</returns>
    Task<string?> AskQuestionAsync(string context, string question, CancellationToken cancellationToken = default);
}
