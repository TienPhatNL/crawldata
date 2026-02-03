using Microsoft.EntityFrameworkCore;
using WebCrawlerService.Domain.Interfaces;
using WebCrawlerService.Domain.Entities;
using WebCrawlerService.Domain.Enums;
using WebCrawlerService.Infrastructure.Contexts;

namespace WebCrawlerService.Infrastructure.Repositories;

public class CrawlerAgentRepository : Repository<CrawlerAgent>, ICrawlerAgentRepository
{
    public CrawlerAgentRepository(CrawlerDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<CrawlerAgent>> GetAvailableAgentsAsync(CrawlerType type, CancellationToken cancellationToken = default)
    {
        return await GetAsync(
            filter: a => a.Type == type && a.Status == AgentStatus.Available,
            orderBy: q => q.OrderBy(a => a.ActiveJobs),
            cancellationToken: cancellationToken);
    }

    public async Task<CrawlerAgent?> GetAgentByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await FindFirstAsync(a => a.Name == name, cancellationToken);
    }

    public async Task<IEnumerable<CrawlerAgent>> GetAgentsByTypeAsync(CrawlerType type, CancellationToken cancellationToken = default)
    {
        return await GetAsync(
            filter: a => a.Type == type,
            orderBy: q => q.OrderBy(a => a.Name),
            cancellationToken: cancellationToken);
    }

    public async Task<CrawlerAgent?> GetLeastBusyAgentAsync(CrawlerType type, CancellationToken cancellationToken = default)
    {
        return await GetQueryable(
            filter: a => a.Type == type && a.Status == AgentStatus.Available)
            .OrderBy(a => a.ActiveJobs)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> GetActiveJobCountForAgentAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        var agent = await GetByIdAsync(agentId, cancellationToken);
        //return agent?.ActiveJobs ?? 0;
        return 0;
    }
}