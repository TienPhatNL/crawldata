using System.Text.Json.Serialization;

namespace WebCrawlerService.Domain.Models.Crawl4AI;

/// <summary>
/// Result of a crawl job submission
/// </summary>
public class CrawlSubmissionResult
{
    /// <summary>
    /// Whether the job was accepted by the agent (either queued or completed)
    /// </summary>
    public bool IsAccepted { get; set; }

    /// <summary>
    /// Whether the job was completed synchronously (HTTP 200) instead of queued (HTTP 202)
    /// </summary>
    public bool IsCompletedSynchronously { get; set; }

    /// <summary>
    /// The response data if completed synchronously
    /// </summary>
    public Crawl4AIResponse? SyncResponse { get; set; }

    /// <summary>
    /// Error message if submission failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    public static CrawlSubmissionResult Success(bool sync, Crawl4AIResponse? response = null) => new()
    {
        IsAccepted = true,
        IsCompletedSynchronously = sync,
        SyncResponse = response
    };

    public static CrawlSubmissionResult Failure(string error) => new()
    {
        IsAccepted = false,
        ErrorMessage = error
    };
}
