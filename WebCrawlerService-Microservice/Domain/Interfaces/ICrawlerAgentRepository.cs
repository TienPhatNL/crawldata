using WebCrawlerService.Domain.Entities;
using WebCrawlerService.Domain.Enums;

namespace WebCrawlerService.Domain.Interfaces;

public interface ICrawlerAgentRepository : IRepository<CrawlerAgent>
{
    Task<IEnumerable<CrawlerAgent>> GetAvailableAgentsAsync(CrawlerType type, CancellationToken cancellationToken = default);
    Task<CrawlerAgent?> GetAgentByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<CrawlerAgent>> GetAgentsByTypeAsync(CrawlerType type, CancellationToken cancellationToken = default);
    Task<CrawlerAgent?> GetLeastBusyAgentAsync(CrawlerType type, CancellationToken cancellationToken = default);
    Task<int> GetActiveJobCountForAgentAsync(Guid agentId, CancellationToken cancellationToken = default);
}