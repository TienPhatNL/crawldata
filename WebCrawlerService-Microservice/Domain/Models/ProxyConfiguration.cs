namespace WebCrawlerService.Domain.Models;

/// <summary>
/// Configuration for a proxy server
/// </summary>
public class ProxyConfiguration
{
    /// <summary>
    /// Proxy URL (e.g., http://proxy.brightdata.com:22225)
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Proxy username for authentication
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Proxy password for authentication
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Proxy type (HTTP, SOCKS5, etc.)
    /// </summary>
    public string Type { get; set; } = "HTTP";

    /// <summary>
    /// Geographic region of proxy (US, VN, UK, etc.)
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Whether this proxy is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Number of consecutive failures
    /// </summary>
    public int FailureCount { get; set; } = 0;

    /// <summary>
    /// Last time this proxy was used
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Last successful request timestamp
    /// </summary>
    public DateTime? LastSuccessAt { get; set; }

    /// <summary>
    /// Average response time in milliseconds
    /// </summary>
    public int AverageResponseTimeMs { get; set; } = 0;
}
