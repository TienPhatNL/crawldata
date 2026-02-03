using WebCrawlerService.Domain.Models;
using WebCrawlerService.Application.DTOs.DataVisualization;

namespace WebCrawlerService.Application.Services.Crawl4AI;

/// <summary>
/// Orchestration service for intelligent crawling
/// Coordinates prompt analysis, navigation planning, and data extraction
/// </summary>
public interface ISmartCrawlerOrchestrationService
{
    /// <summary>
    /// Execute intelligent crawl based on natural language prompt
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="prompt">Natural language crawl request</param>
    /// <param name="url">Target URL</param>
    /// <param name="assignmentId">Optional assignment ID for tracking</param>
    /// <param name="groupId">Optional group ID for collaborative crawls</param>
    /// <param name="conversationThreadId">Optional conversation thread ID for chat integration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Crawl job result with extracted data</returns>
    Task<CrawlJobResult> ExecuteIntelligentCrawlAsync(
        Guid userId,
        string prompt,
        string url,
        Guid? assignmentId = null,
        Guid? groupId = null,
        Guid? conversationThreadId = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Execute intelligent crawl from Kafka event (ClassroomService request)
    /// Processes smart crawl requests asynchronously in background
    /// </summary>
    /// <param name="crawlRequest">Smart crawl request event from Kafka</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task ExecuteIntelligentCrawlFromEventAsync(
        object crawlRequest,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Get crawl job result
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Crawl job result or null</returns>
    Task<CrawlJobResult?> GetJobResultAsync(
        Guid jobId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Process a user message for potential RAG response
    /// </summary>
    Task<string?> ProcessUserMessageAsync(Guid conversationId, string userMessage, string? csvContext = null, CancellationToken cancellationToken = default);

    Task<CrawlJobSummaryDto?> GetConversationSummaryAsync(
        Guid conversationId,
        string prompt,
        CancellationToken cancellationToken = default);
}
