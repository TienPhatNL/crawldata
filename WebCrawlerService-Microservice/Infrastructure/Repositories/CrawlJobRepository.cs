using Microsoft.EntityFrameworkCore;
using WebCrawlerService.Domain.Interfaces;
using WebCrawlerService.Domain.Entities;
using WebCrawlerService.Domain.Enums;
using WebCrawlerService.Infrastructure.Contexts;

namespace WebCrawlerService.Infrastructure.Repositories;

public class CrawlJobRepository : Repository<CrawlJob>, ICrawlJobRepository
{
    public CrawlJobRepository(CrawlerDbContext context) : base(context)
    {
    }

    //public async Task<IEnumerable<CrawlJob>> GetJobsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    //{
    //    return await GetAsync(
    //        filter: j => j.UserId == userId,
    //        orderBy: q => q.OrderByDescending(j => j.CreatedAt),
    //        cancellationToken: cancellationToken,
    //        includes: j => j.Results, j => j.AssignedAgent);
    //}

    public async Task<IEnumerable<CrawlJob>> GetJobsByStatusAsync(JobStatus status, CancellationToken cancellationToken = default)
    {
        return await GetAsync(
            filter: j => j.Status == status,
            orderBy: q => q.OrderBy(j => j.CreatedAt),
            cancellationToken: cancellationToken,
            includes: j => j.AssignedAgent);
    }

    public async Task<IEnumerable<CrawlJob>> GetPendingJobsAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        return await GetAsync(
            filter: j => j.Status == JobStatus.Pending,
            orderBy: q => q.OrderByDescending(j => j.Priority).ThenBy(j => j.CreatedAt),
            take: limit,
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<CrawlJob>> GetJobsByPriorityAsync(Priority priority, CancellationToken cancellationToken = default)
    {
        return await GetAsync(
            filter: j => j.Priority == priority,
            orderBy: q => q.OrderBy(j => j.CreatedAt),
            cancellationToken: cancellationToken);
    }

    public async Task<CrawlJob?> GetJobWithResultsAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        return await GetQueryable(j => j.Id == jobId, j => j.Results, j => j.AssignedAgent)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> GetActiveJobCountByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await CountAsync(j => j.UserId == userId && 
            (j.Status == JobStatus.Pending || j.Status == JobStatus.InProgress), cancellationToken);
    }

    public async Task<IEnumerable<CrawlJob>> GetJobsRequiringRetryAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync(
            filter: j => j.Status == JobStatus.Failed && j.RetryCount < j.MaxRetries,
            orderBy: q => q.OrderByDescending(j => j.Priority).ThenBy(j => j.FailedAt),
            cancellationToken: cancellationToken);
    }
}