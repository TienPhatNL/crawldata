using Microsoft.EntityFrameworkCore;

using WebCrawlerService.Domain.Entities;
using WebCrawlerService.Domain.Enums;
using WebCrawlerService.Domain.Interfaces;
using WebCrawlerService.Infrastructure.Contexts;

namespace WebCrawlerService.Infrastructure.Repositories;

public class CrawlTemplateRepository : Repository<CrawlTemplate>, ICrawlTemplateRepository
{
    public CrawlTemplateRepository(CrawlerDbContext context) : base(context)
    {
    }

    public async Task<CrawlTemplate?> GetByDomainPatternAsync(string url, CancellationToken cancellationToken = default)
    {
        // Find the best matching template for the given URL
        var activeTemplates = await GetQueryable(
            filter: t => t.IsActive && t.IsValidated)
            .OrderByDescending(t => t.SuccessRate)
            .ToListAsync(cancellationToken);

        foreach (var template in activeTemplates)
        {
            // Simple wildcard matching (can be enhanced with regex)
            var pattern = template.DomainPattern
                .Replace("*", ".*")
                .Replace("?", ".");

            if (System.Text.RegularExpressions.Regex.IsMatch(url, pattern,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                return template;
            }
        }

        return null;
    }

    public async Task<IEnumerable<CrawlTemplate>> GetActiveTemplatesAsync(CancellationToken cancellationToken = default)
    {
        return await FindAsync(t => t.IsActive, cancellationToken);
    }

    public async Task<IEnumerable<CrawlTemplate>> GetTemplatesByTypeAsync(TemplateType type, CancellationToken cancellationToken = default)
    {
        return await FindAsync(t => t.Type == type && t.IsActive, cancellationToken);
    }

    public async Task<IEnumerable<CrawlTemplate>> GetUserTemplatesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await FindAsync(t => t.CreatedBy == userId, cancellationToken);
    }

    public async Task<IEnumerable<CrawlTemplate>> GetPublicTemplatesAsync(CancellationToken cancellationToken = default)
    {
        return await FindAsync(t => t.IsPublic && t.IsActive, cancellationToken);
    }

    public async Task<IEnumerable<CrawlTemplate>> GetSystemTemplatesAsync(CancellationToken cancellationToken = default)
    {
        return await FindAsync(t => t.IsSystemTemplate && t.IsActive, cancellationToken);
    }

    public async Task<CrawlTemplate?> GetLatestVersionAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var template = await GetByIdAsync(templateId, cancellationToken);
        if (template == null) return null;

        // Find the latest version in the chain
        var latest = template;
        while (latest.NewerVersions.Any())
        {
            latest = await GetQueryable(
                filter: t => t.PreviousVersionId == latest.Id && t.IsActive)
                .OrderByDescending(t => t.Version)
                .FirstOrDefaultAsync(cancellationToken) ?? latest;
        }

        return latest;
    }

    public async Task<IEnumerable<CrawlTemplate>> SearchTemplatesAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        return await GetQueryable(
            filter: t => t.IsActive && (
                t.Name.Contains(searchTerm) ||
                t.Description.Contains(searchTerm) ||
                t.DomainPattern.Contains(searchTerm) ||
                t.Tags.Any(tag => tag.Contains(searchTerm))))
            .ToListAsync(cancellationToken);
    }
}
