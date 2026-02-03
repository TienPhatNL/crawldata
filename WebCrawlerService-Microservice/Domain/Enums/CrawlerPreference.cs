namespace WebCrawlerService.Domain.Enums;

/// <summary>
/// User preference for crawler selection
/// </summary>
public enum CrawlerPreference
{
    /// <summary>
    /// Automatically select the best crawler based on URL and prompt
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Force use of Playwright for dynamic AI-powered extraction
    /// </summary>
    PlaywrightAI = 1,

    /// <summary>
    /// Force use of template-based extraction (if template exists)
    /// </summary>
    Template = 2,

    /// <summary>
    /// Force use of app-specific crawler (Shopee, Lazada, etc.)
    /// </summary>
    AppSpecific = 3,

    /// <summary>
    /// Force use of basic HTTP client (fastest, least reliable)
    /// </summary>
    HttpClient = 4
}
