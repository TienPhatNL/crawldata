using WebCrawlerService.Domain.Entities;
using WebCrawlerService.Domain.Enums;

namespace WebCrawlerService.Domain.Interfaces;

public interface IDomainPolicyRepository : IRepository<DomainPolicy>
{
    Task<IEnumerable<DomainPolicy>> GetActivePoliciesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<DomainPolicy>> GetPoliciesByTypeAsync(DomainPolicyType type, CancellationToken cancellationToken = default);
    Task<DomainPolicy?> GetPolicyForDomainAsync(string domain, CancellationToken cancellationToken = default);
    Task<bool> IsDomainAllowedAsync(string domain, UserRole userRole, CancellationToken cancellationToken = default);
    Task<IEnumerable<DomainPolicy>> GetWhitelistPoliciesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<DomainPolicy>> GetBlacklistPoliciesAsync(CancellationToken cancellationToken = default);
}