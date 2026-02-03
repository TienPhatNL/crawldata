using WebCrawlerService.Domain.Common;
using WebCrawlerService.Domain.Enums;

namespace WebCrawlerService.Domain.Entities;

/// <summary>
/// Represents a reusable crawling template for specific websites or patterns
/// </summary>
public class CrawlTemplate : BaseAuditableEntity
{
    /// <summary>
    /// Template name (e.g., "Shopee Product Page")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL pattern to match (e.g., "*.shopee.vn/product/*")
    /// Supports wildcards for flexible matching
    /// </summary>
    public string DomainPattern { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of what this template does
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Type of template (Web, MobileApp, API, SPA, Dynamic)
    /// </summary>
    public TemplateType Type { get; set; } = TemplateType.WebPage;

    /// <summary>
    /// Recommended crawler for this template
    /// </summary>
    public CrawlerType RecommendedCrawler { get; set; } = CrawlerType.Playwright;

    /// <summary>
    /// JSON configuration containing selectors, wait conditions, etc.
    /// Serialized TemplateConfiguration object
    /// </summary>
    public string ConfigurationJson { get; set; } = "{}";

    /// <summary>
    /// Sample URLs for testing and validation
    /// </summary>
    public string[] SampleUrls { get; set; } = Array.Empty<string>();

    // Versioning
    /// <summary>
    /// Template version number (semantic versioning)
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Reference to previous version (for version history)
    /// </summary>
    public Guid? PreviousVersionId { get; set; }

    /// <summary>
    /// Whether this template is currently active and usable
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this template has been validated against live URLs
    /// </summary>
    public bool IsValidated { get; set; } = false;

    /// <summary>
    /// Last time this template was tested/validated
    /// </summary>
    public DateTime? LastTestedAt { get; set; }

    /// <summary>
    /// When this template was deprecated/deactivated
    /// </summary>
    public DateTime? DeprecatedAt { get; set; }

    /// <summary>
    /// Last validation error message (if validation failed)
    /// </summary>
    public string? LastValidationError { get; set; }

    // Rate Limiting & Authentication
    /// <summary>
    /// Delay in milliseconds between requests using this template
    /// </summary>
    public int RateLimitDelayMs { get; set; } = 1000;

    /// <summary>
    /// Whether this template requires authentication
    /// </summary>
    public bool RequiresAuthentication { get; set; } = false;

    /// <summary>
    /// API endpoint pattern (for API-type templates)
    /// </summary>
    public string? ApiEndpointPattern { get; set; }

    /// <summary>
    /// Mobile API provider if template is for mobile API crawling
    /// </summary>
    public MobileApiProvider? MobileApiProvider { get; set; }

    /// <summary>
    /// Serialized MobileApiConfiguration for this template
    /// </summary>
    public string? MobileApiConfigJson { get; set; }

    // Metadata & Analytics
    // Note: CreatedBy is inherited from BaseAuditableEntity

    /// <summary>
    /// Number of times this template has been used
    /// </summary>
    public int UsageCount { get; set; } = 0;

    /// <summary>
    /// Success rate of this template (0.0 to 1.0)
    /// </summary>
    public double SuccessRate { get; set; } = 0.0;

    /// <summary>
    /// Average extraction time in milliseconds
    /// </summary>
    public int AverageExtractionTimeMs { get; set; } = 0;

    /// <summary>
    /// Tags for categorization and search
    /// </summary>
    public string[] Tags { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Whether this is a system template (pre-built) or user-created
    /// </summary>
    public bool IsSystemTemplate { get; set; } = false;

    /// <summary>
    /// Whether this template is public (visible to all users)
    /// </summary>
    public bool IsPublic { get; set; } = false;

    // Navigation Properties
    /// <summary>
    /// Reference to previous version of this template
    /// </summary>
    public virtual CrawlTemplate? PreviousVersion { get; set; }

    /// <summary>
    /// Newer versions of this template
    /// </summary>
    public virtual ICollection<CrawlTemplate> NewerVersions { get; set; } = new List<CrawlTemplate>();

    /// <summary>
    /// Crawl jobs that used this template
    /// </summary>
    public virtual ICollection<CrawlJob> CrawlJobs { get; set; } = new List<CrawlJob>();
}
