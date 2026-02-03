using WebCrawlerService.Domain.Common;

namespace WebCrawlerService.Domain.Events;

/// <summary>
/// Published when data extraction events occur during intelligent crawling
/// </summary>
public class JobExtractionEvent : BaseEvent
{
    public Guid JobId { get; }
    public Guid UserId { get; }
    public string ExtractionEventType { get; } // DataExtractionStarted, DataExtractionCompleted
    public int? TotalItemsExtracted { get; }
    public int? PagesProcessed { get; }
    public bool? ExtractionSuccessful { get; }
    public string? ErrorMessage { get; }
    public double? ExecutionTimeMs { get; }
    public DateTime OccurredAt { get; }

    public JobExtractionEvent(
        Guid jobId,
        Guid userId,
        string extractionEventType,
        int? totalItemsExtracted = null,
        int? pagesProcessed = null,
        bool? extractionSuccessful = null,
        string? errorMessage = null,
        double? executionTimeMs = null,
        DateTime? occurredAt = null)
    {
        JobId = jobId;
        UserId = userId;
        ExtractionEventType = extractionEventType;
        TotalItemsExtracted = totalItemsExtracted;
        PagesProcessed = pagesProcessed;
        ExtractionSuccessful = extractionSuccessful;
        ErrorMessage = errorMessage;
        ExecutionTimeMs = executionTimeMs;
        OccurredAt = occurredAt ?? DateTime.UtcNow;
    }
}
