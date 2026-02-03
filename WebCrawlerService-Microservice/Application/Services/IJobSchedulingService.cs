using WebCrawlerService.Domain.Entities;
using WebCrawlerService.Domain.Enums;

namespace WebCrawlerService.Application.Services;

public interface IJobSchedulingService
{
    Task<CrawlJob?> GetNextJobForProcessingAsync(CancellationToken cancellationToken = default);
    Task<bool> ScheduleJobAsync(CrawlJob job, CancellationToken cancellationToken = default);
    Task<bool> RescheduleFailedJobAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CrawlJob>> GetJobsForRetryAsync(CancellationToken cancellationToken = default);
    Task<bool> UpdateJobPriorityAsync(Guid jobId, Priority priority, CancellationToken cancellationToken = default);
    Task<int> GetQueueLengthAsync(Priority? priority = null, CancellationToken cancellationToken = default);
}