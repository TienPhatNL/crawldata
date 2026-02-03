using WebCrawlerService.Domain.Entities;

namespace WebCrawlerService.Domain.Interfaces;

public interface ICrawlResultRepository : IRepository<CrawlResult>
{
    Task<IEnumerable<CrawlResult>> GetResultsByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<CrawlResult?> GetResultByUrlAsync(string url, CancellationToken cancellationToken = default);
    Task<IEnumerable<CrawlResult>> GetResultsByContentHashAsync(string contentHash, CancellationToken cancellationToken = default);
    Task<IEnumerable<CrawlResult>> GetSuccessfulResultsByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CrawlResult>> GetFailedResultsByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<long> GetTotalContentSizeByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default);
}