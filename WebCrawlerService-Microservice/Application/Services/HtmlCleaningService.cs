using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace WebCrawlerService.Application.Services;

/// <summary>
/// Service for deep HTML cleaning to reduce token usage while preserving content
/// Removes JavaScript, CSS, SVG, comments, and non-essential attributes
/// Expected: 50-70% reduction in HTML size
/// </summary>
public class HtmlCleaningService : IHtmlCleaningService
{
    private readonly ILogger<HtmlCleaningService> _logger;

    public HtmlCleaningService(ILogger<HtmlCleaningService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Perform deep cleaning on HTML content
    /// </summary>
    /// <param name="html">Raw HTML content</param>
    /// <returns>Cleaned HTML with reduced size</returns>
    public string DeepClean(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return html;

        try
        {
            var originalLength = html.Length;
            _logger.LogDebug("Deep cleaning HTML ({Length} chars)...", originalLength);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // 1. Remove script tags (except JSON-LD structured data which is useful)
            var scriptCount = RemoveScriptTags(doc);

            // 2. Remove style tags
            var styleCount = RemoveStyleTags(doc);

            // 3. Remove SVG elements (usually large and not useful for text extraction)
            var svgCount = RemoveSvgElements(doc);

            // 4. Remove HTML comments
            var commentCount = RemoveComments(doc);

            // 5. Remove inline styles and non-essential attributes
            CleanAttributes(doc);

            var cleanedHtml = doc.DocumentNode.OuterHtml;
            var cleanedLength = cleanedHtml.Length;
            var reductionPercent = originalLength > 0
                ? 100 - (int)((double)cleanedLength / originalLength * 100)
                : 0;

            _logger.LogDebug(
                "Cleaned HTML: Removed {ScriptCount} scripts, {StyleCount} styles, {SvgCount} SVGs, {CommentCount} comments",
                scriptCount, styleCount, svgCount, commentCount);
            _logger.LogDebug(
                "Size reduction: {OriginalLength} -> {CleanedLength} chars ({ReductionPercent}% reduction)",
                originalLength, cleanedLength, reductionPercent);

            return cleanedHtml;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "HTML cleaning failed - using original HTML");
            return html; // Fallback to original HTML if cleaning fails
        }
    }

    /// <summary>
    /// Remove script tags while preserving JSON-LD structured data
    /// </summary>
    private int RemoveScriptTags(HtmlDocument doc)
    {
        var scriptNodes = doc.DocumentNode.SelectNodes("//script");
        if (scriptNodes == null)
            return 0;

        var removed = 0;
        foreach (var script in scriptNodes.ToList())
        {
            // Keep JSON-LD for structured data extraction
            var type = script.GetAttributeValue("type", "");
            if (type == "application/ld+json")
                continue;

            script.Remove();
            removed++;
        }

        return removed;
    }

    /// <summary>
    /// Remove all style tags
    /// </summary>
    private int RemoveStyleTags(HtmlDocument doc)
    {
        var styleNodes = doc.DocumentNode.SelectNodes("//style");
        if (styleNodes == null)
            return 0;

        var removed = styleNodes.Count;
        foreach (var style in styleNodes.ToList())
        {
            style.Remove();
        }

        return removed;
    }

    /// <summary>
    /// Remove SVG elements (usually large and not useful for text extraction)
    /// </summary>
    private int RemoveSvgElements(HtmlDocument doc)
    {
        var svgNodes = doc.DocumentNode.SelectNodes("//svg");
        if (svgNodes == null)
            return 0;

        var removed = svgNodes.Count;
        foreach (var svg in svgNodes.ToList())
        {
            svg.Remove();
        }

        return removed;
    }

    /// <summary>
    /// Remove HTML comments
    /// </summary>
    private int RemoveComments(HtmlDocument doc)
    {
        var commentNodes = doc.DocumentNode.SelectNodes("//comment()");
        if (commentNodes == null)
            return 0;

        var removed = commentNodes.Count;
        foreach (var comment in commentNodes.ToList())
        {
            comment.Remove();
        }

        return removed;
    }

    /// <summary>
    /// Clean non-essential attributes from all tags
    /// Removes: inline styles, data-* attributes, event handlers (onclick, onload, etc.)
    /// </summary>
    private void CleanAttributes(HtmlDocument doc)
    {
        var allNodes = doc.DocumentNode.SelectNodes("//*");
        if (allNodes == null)
            return;

        foreach (var node in allNodes)
        {
            // Collect attributes to remove
            var attributesToRemove = new List<string>();

            foreach (var attr in node.Attributes)
            {
                var attrName = attr.Name.ToLowerInvariant();

                // Remove inline style attribute
                if (attrName == "style")
                {
                    attributesToRemove.Add(attr.Name);
                }
                // Remove data-* attributes (usually for JS interactions)
                else if (attrName.StartsWith("data-"))
                {
                    attributesToRemove.Add(attr.Name);
                }
                // Remove event handlers (onclick, onload, etc.)
                else if (attrName.StartsWith("on"))
                {
                    attributesToRemove.Add(attr.Name);
                }
            }

            // Remove collected attributes
            foreach (var attrName in attributesToRemove)
            {
                node.Attributes.Remove(attrName);
            }
        }
    }

    /// <summary>
    /// Light clean - only removes scripts and styles, preserves structure
    /// </summary>
    public string LightClean(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return html;

        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            RemoveScriptTags(doc);
            RemoveStyleTags(doc);

            return doc.DocumentNode.OuterHtml;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Light HTML cleaning failed - using original HTML");
            return html;
        }
    }
}
