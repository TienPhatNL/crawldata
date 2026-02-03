using WebCrawlerService.Domain.Enums;

namespace WebCrawlerService.Domain.Models;

/// <summary>
/// Configuration for mobile app API crawling
/// </summary>
public class MobileApiConfiguration
{
    /// <summary>
    /// Mobile API provider type
    /// </summary>
    public MobileApiProvider Provider { get; set; }

    /// <summary>
    /// Base URL for API calls
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// API version (e.g., "v4", "v2")
    /// </summary>
    public string ApiVersion { get; set; } = "v4";

    /// <summary>
    /// Required HTTP headers for authentication
    /// </summary>
    public Dictionary<string, string> RequiredHeaders { get; set; } = new();

    /// <summary>
    /// Default query parameters for all requests
    /// </summary>
    public Dictionary<string, string> DefaultParams { get; set; } = new();

    /// <summary>
    /// Signature algorithm if required (e.g., "HMAC-SHA256")
    /// </summary>
    public string? SignatureAlgorithm { get; set; }

    /// <summary>
    /// Whether requests require cryptographic signatures
    /// </summary>
    public bool RequiresSignature { get; set; }

    /// <summary>
    /// Whether requests require session cookies
    /// </summary>
    public bool RequiresCookies { get; set; }

    /// <summary>
    /// Geographic region for API (VN, SG, TH, etc.)
    /// </summary>
    public string Region { get; set; } = "VN";

    /// <summary>
    /// Maximum requests per minute to avoid rate limiting
    /// </summary>
    public int RateLimitPerMinute { get; set; } = 20;

    /// <summary>
    /// Whether proxy rotation is required
    /// </summary>
    public bool RequiresProxy { get; set; } = true;
}
