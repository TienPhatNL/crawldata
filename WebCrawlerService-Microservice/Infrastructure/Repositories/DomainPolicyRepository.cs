using Microsoft.EntityFrameworkCore;
using WebCrawlerService.Domain.Interfaces;
using WebCrawlerService.Domain.Entities;
using WebCrawlerService.Domain.Enums;
using WebCrawlerService.Infrastructure.Contexts;

namespace WebCrawlerService.Infrastructure.Repositories;

public class DomainPolicyRepository : Repository<DomainPolicy>, IDomainPolicyRepository
{
    public DomainPolicyRepository(CrawlerDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<DomainPolicy>> GetActivePoliciesAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync(
            filter: p => p.IsActive,
            orderBy: q => q.OrderBy(p => p.PolicyType),
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<DomainPolicy>> GetPoliciesByTypeAsync(DomainPolicyType type, CancellationToken cancellationToken = default)
    {
        return await GetAsync(
            filter: p => p.PolicyType == type && p.IsActive,
            orderBy: q => q.OrderBy(p => p.DomainPattern),
            cancellationToken: cancellationToken);
    }

    public async Task<DomainPolicy?> GetPolicyForDomainAsync(string domain, CancellationToken cancellationToken = default)
    {
        return await GetQueryable(filter: p => p.IsActive)
            .Where(p => domain.Contains(p.DomainPattern) || p.DomainPattern.Contains(domain))
            .OrderBy(p => p.PolicyType)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> IsDomainAllowedAsync(string domain, UserRole userRole, CancellationToken cancellationToken = default)
    {
        // Check blacklist first
        var blacklistPolicy = await GetQueryable(
            filter: p => p.PolicyType == DomainPolicyType.Blacklist && p.IsActive)
            .FirstOrDefaultAsync(p => domain.Contains(p.DomainPattern), cancellationToken);

        if (blacklistPolicy != null)
        {
            return false;
        }

        // Check whitelist policies
        var whitelistPolicies = await GetPoliciesByTypeAsync(DomainPolicyType.Whitelist, cancellationToken);
        
        if (whitelistPolicies.Any())
        {
            return whitelistPolicies.Any(p => 
                domain.Contains(p.DomainPattern) && 
                (p.AllowedRoles == null || p.AllowedRoles.Contains(userRole)));
        }

        // If no specific policies, default is allowed
        return true;
    }

    public async Task<IEnumerable<DomainPolicy>> GetWhitelistPoliciesAsync(CancellationToken cancellationToken = default)
    {
        return await GetPoliciesByTypeAsync(DomainPolicyType.Whitelist, cancellationToken);
    }

    public async Task<IEnumerable<DomainPolicy>> GetBlacklistPoliciesAsync(CancellationToken cancellationToken = default)
    {
        return await GetPoliciesByTypeAsync(DomainPolicyType.Blacklist, cancellationToken);
    }
}