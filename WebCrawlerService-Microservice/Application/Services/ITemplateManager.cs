using WebCrawlerService.Domain.Entities;
using WebCrawlerService.Domain.Models;

namespace WebCrawlerService.Application.Services;

/// <summary>
/// Service for managing crawl templates
/// </summary>
public interface ITemplateManager
{
    /// <summary>
    /// Create a new template from extraction strategy
    /// </summary>
    Task<CrawlTemplate> CreateTemplateAsync(
        string name,
        string domainPattern,
        ExtractionStrategy strategy,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update existing template
    /// </summary>
    Task<CrawlTemplate> UpdateTemplateAsync(
        Guid templateId,
        TemplateConfiguration configuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate template against sample URLs
    /// </summary>
    Task<(bool IsValid, string? ErrorMessage)> ValidateTemplateAsync(
        Guid templateId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create new version of existing template
    /// </summary>
    Task<CrawlTemplate> CreateNewVersionAsync(
        Guid templateId,
        string changeDescription,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Auto-repair template based on recent failures
    /// </summary>
    Task<CrawlTemplate?> AutoRepairTemplateAsync(
        Guid templateId,
        string errorDetails,
        CancellationToken cancellationToken = default);
}
