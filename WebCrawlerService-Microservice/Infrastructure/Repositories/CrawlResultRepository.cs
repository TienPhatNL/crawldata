using Microsoft.EntityFrameworkCore;
using WebCrawlerService.Domain.Interfaces;
using WebCrawlerService.Domain.Entities;
using WebCrawlerService.Infrastructure.Contexts;

namespace WebCrawlerService.Infrastructure.Repositories;

public class CrawlResultRepository : Repository<CrawlResult>, ICrawlResultRepository
{
    public CrawlResultRepository(CrawlerDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<CrawlResult>> GetResultsByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        return await GetAsync(
            filter: r => r.CrawlJobId == jobId,
            orderBy: q => q.OrderBy(r => r.CrawledAt),
            cancellationToken: cancellationToken);
    }

    public async Task<CrawlResult?> GetResultByUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        return await FindFirstAsync(r => r.Url == url, cancellationToken);
    }

    public async Task<IEnumerable<CrawlResult>> GetResultsByContentHashAsync(string contentHash, CancellationToken cancellationToken = default)
    {
        return await GetAsync(
            filter: r => r.ContentHash == contentHash,
            orderBy: q => q.OrderByDescending(r => r.CrawledAt),
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<CrawlResult>> GetSuccessfulResultsByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        return await GetAsync(
            filter: r => r.CrawlJobId == jobId && r.IsSuccess,
            orderBy: q => q.OrderBy(r => r.CrawledAt),
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<CrawlResult>> GetFailedResultsByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        return await GetAsync(
            filter: r => r.CrawlJobId == jobId && !r.IsSuccess,
            orderBy: q => q.OrderBy(r => r.CrawledAt),
            cancellationToken: cancellationToken);
    }

    public async Task<long> GetTotalContentSizeByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        return await GetQueryable(filter: r => r.CrawlJobId == jobId && r.IsSuccess)
            .SumAsync(r => r.ContentSize, cancellationToken);
    }
}