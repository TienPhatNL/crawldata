using ClassroomService.Domain.DTOs;

namespace ClassroomService.Domain.Interfaces;

/// <summary>
/// Service for integrating with WebCrawlerService-Microservice
/// </summary>
public interface ICrawlerIntegrationService
{
    /// <summary>
    /// Initiate a new crawl job for an assignment/group
    /// </summary>
    Task<CrawlJobResponse> InitiateCrawlAsync(InitiateCrawlRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiate a smart crawl with AI-powered analysis
    /// </summary>
    Task<CrawlJobResponse> InitiateSmartCrawlAsync(SmartCrawlRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the status of a crawl job
    /// </summary>
    Task<CrawlJobStatusResponse> GetCrawlStatusAsync(Guid crawlJobId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get detailed results of a completed crawl job
    /// </summary>
    Task<List<CrawlResultDetailDto>> GetCrawlResultsAsync(Guid crawlJobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ask a question about a specific conversation (RAG)
    /// </summary>
    Task<string?> AskQuestionAsync(Guid conversationId, string question, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get crawl summary
    /// </summary>

    /// <summary>
    /// Cancel a running crawl job
    /// </summary>
    Task<bool> CancelCrawlAsync(Guid crawlJobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all crawl jobs for a specific assignment
    /// </summary>
    Task<List<CrawlJobStatusResponse>> GetAssignmentCrawlsAsync(Guid assignmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all crawl jobs for a specific group
    /// </summary>
    Task<List<CrawlJobStatusResponse>> GetGroupCrawlsAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Share a crawl job with group members
    /// </summary>
    Task<bool> ShareCrawlWithGroupAsync(Guid crawlJobId, Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get AI-generated summary of crawl results
    /// </summary>
    Task<string> GetCrawlSummaryAsync(Guid crawlJobId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get crawl job details including conversation name
    /// </summary>
    Task<CrawlJobDetailsDto?> GetCrawlJobAsync(Guid crawlJobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get full summary including charts
    /// </summary>
    Task<CrawlJobSummaryDto?> GetFullCrawlSummaryAsync(Guid crawlJobId, string? accessToken = null, string? prompt = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get conversation-level summary and charts (aggregates multiple jobs)
    /// </summary>
    Task<CrawlJobSummaryDto?> GetConversationSummaryAsync(
        Guid conversationId,
        string? accessToken,
        string? prompt,
        CancellationToken cancellationToken);
}
