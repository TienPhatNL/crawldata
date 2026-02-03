namespace WebCrawlerService.Application.DTOs.Charts;

/// <summary>
/// Base query parameters for chart data filtering
/// </summary>
public class ChartQueryParams
{
    /// <summary>
    /// User ID filter (null for all users, required for non-admin users)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Start date for data range (UTC)
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date for data range (UTC)
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Filter by crawler type (HttpClient, Selenium, Playwright, Scrapy, Crawl4AI)
    /// </summary>
    public string? CrawlerType { get; set; }

    /// <summary>
    /// Filter by job status (Pending, Running, Completed, Failed, Cancelled)
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Maximum number of data points to return (for performance)
    /// Default: 100, Max: 1000
    /// </summary>
    public int MaxDataPoints { get; set; } = 100;

    /// <summary>
    /// Whether to use cached data (default: true)
    /// </summary>
    public bool UseCache { get; set; } = true;

    /// <summary>
    /// Cache TTL in seconds (default: 300 = 5 minutes)
    /// </summary>
    public int CacheTtl { get; set; } = 300;
}
