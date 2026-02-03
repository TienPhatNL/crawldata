namespace WebCrawlerService.Application.Services;

/// <summary>
/// Service interface for HTML cleaning operations
/// </summary>
public interface IHtmlCleaningService
{
    /// <summary>
    /// Perform deep cleaning on HTML content
    /// Removes scripts, styles, SVGs, comments, and non-essential attributes
    /// Expected: 50-70% reduction in HTML size
    /// </summary>
    /// <param name="html">Raw HTML content</param>
    /// <returns>Cleaned HTML with reduced size</returns>
    string DeepClean(string html);

    /// <summary>
    /// Light clean - only removes scripts and styles, preserves structure
    /// </summary>
    /// <param name="html">Raw HTML content</param>
    /// <returns>Lightly cleaned HTML</returns>
    string LightClean(string html);
}
