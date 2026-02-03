using WebCrawlerService.Domain.Entities;
using WebCrawlerService.Domain.Enums;

namespace WebCrawlerService.Domain.Interfaces;

public interface ICrawlJobRepository : IRepository<CrawlJob>
{
    //Task<IEnumerable<CrawlJob>> GetJobsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CrawlJob>> GetJobsByStatusAsync(JobStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<CrawlJob>> GetPendingJobsAsync(int limit = 10, CancellationToken cancellationToken = default);
    Task<IEnumerable<CrawlJob>> GetJobsByPriorityAsync(Priority priority, CancellationToken cancellationToken = default);
    Task<CrawlJob?> GetJobWithResultsAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<int> GetActiveJobCountByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CrawlJob>> GetJobsRequiringRetryAsync(CancellationToken cancellationToken = default);
}