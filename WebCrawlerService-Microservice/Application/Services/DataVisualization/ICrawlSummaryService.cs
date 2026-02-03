using WebCrawlerService.Application.DTOs.DataVisualization;

namespace WebCrawlerService.Application.Services.DataVisualization;

/// <summary>
/// Generates lightweight crawl summaries backed by persisted CrawlResults so downstream services can render insights quickly.
/// </summary>
public interface ICrawlSummaryService
{
    Task<CrawlJobSummaryDto> GenerateSummaryAsync(Guid jobId, string? prompt = null, CancellationToken cancellationToken = default);
}
