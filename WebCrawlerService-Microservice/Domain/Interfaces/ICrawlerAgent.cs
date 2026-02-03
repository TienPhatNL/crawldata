using WebCrawlerService.Domain.Entities;

namespace WebCrawlerService.Domain.Interfaces;

/// <summary>
/// Interface for crawler agents that execute crawl jobs
/// </summary>
public interface ICrawlerAgent
{
    /// <summary>
    /// Execute a crawl job and return results
    /// </summary>
    /// <param name="job">The crawl job to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of crawl results</returns>
    Task<List<CrawlResult>> ExecuteAsync(CrawlJob job, CancellationToken cancellationToken = default);
}
