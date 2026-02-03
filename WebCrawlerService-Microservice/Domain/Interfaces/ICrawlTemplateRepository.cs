using WebCrawlerService.Domain.Entities;
using WebCrawlerService.Domain.Enums;

namespace WebCrawlerService.Domain.Interfaces;

public interface ICrawlTemplateRepository : IRepository<CrawlTemplate>
{
    Task<CrawlTemplate?> GetByDomainPatternAsync(string url, CancellationToken cancellationToken = default);
    Task<IEnumerable<CrawlTemplate>> GetActiveTemplatesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<CrawlTemplate>> GetTemplatesByTypeAsync(TemplateType type, CancellationToken cancellationToken = default);
    Task<IEnumerable<CrawlTemplate>> GetUserTemplatesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CrawlTemplate>> GetPublicTemplatesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<CrawlTemplate>> GetSystemTemplatesAsync(CancellationToken cancellationToken = default);
    Task<CrawlTemplate?> GetLatestVersionAsync(Guid templateId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CrawlTemplate>> SearchTemplatesAsync(string searchTerm, CancellationToken cancellationToken = default);
}
