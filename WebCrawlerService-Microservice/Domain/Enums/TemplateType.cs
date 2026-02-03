namespace WebCrawlerService.Domain.Enums;

/// <summary>
/// Defines the type of crawl template
/// </summary>
public enum TemplateType
{
    /// <summary>
    /// Standard web page with HTML structure
    /// </summary>
    WebPage = 0,

    /// <summary>
    /// Mobile app emulation or mobile-specific crawling
    /// </summary>
    MobileApp = 1,

    /// <summary>
    /// Direct API endpoint crawling
    /// </summary>
    API = 2,

    /// <summary>
    /// Single Page Application (heavy JavaScript)
    /// </summary>
    SPA = 3,

    /// <summary>
    /// Dynamic content requiring JavaScript execution
    /// </summary>
    Dynamic = 4
}
