namespace WebCrawlerService.Domain.Models;

/// <summary>
/// Configuration for Shopee mobile API integration
/// </summary>
public class ShopeeApiConfiguration
{
    /// <summary>
    /// Base URL for Shopee API (e.g., https://shopee.vn)
    /// </summary>
    public string BaseUrl { get; set; } = "https://shopee.vn";

    /// <summary>
    /// User-Agent header mimicking Shopee mobile app
    /// </summary>
    public string UserAgent { get; set; } = "Shopee/2.98.21 Android/11";

    /// <summary>
    /// Language code for API responses (vi, en, th, etc.)
    /// </summary>
    public string Language { get; set; } = "vi";

    /// <summary>
    /// Region/country code (VN, SG, TH, PH, MY, etc.)
    /// </summary>
    public string Region { get; set; } = "VN";

    /// <summary>
    /// Session cookies for authenticated requests (SPC_EC, SPC_F, SPC_U)
    /// Optional - some endpoints work without authentication
    /// </summary>
    public string? SessionCookies { get; set; }

    /// <summary>
    /// Whether to use proxy rotation for requests
    /// </summary>
    public bool UseProxy { get; set; } = true;

    /// <summary>
    /// Delay in milliseconds between consecutive requests
    /// </summary>
    public int RequestDelayMs { get; set; } = 500;

    /// <summary>
    /// Maximum number of requests per minute to avoid rate limiting
    /// </summary>
    public int RateLimitPerMinute { get; set; } = 20;

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to enable request/response logging for debugging
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Maximum number of retry attempts for failed requests
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Base delay for exponential backoff in seconds
    /// </summary>
    public int RetryBaseDelaySeconds { get; set; } = 2;
}
