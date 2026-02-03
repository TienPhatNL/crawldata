using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace WebCrawlerService.Application.Services;

/// <summary>
/// Service for chunking HTML content while respecting element boundaries
/// Prevents splitting products/articles mid-element for better extraction
/// </summary>
public class ChunkingService : IChunkingService
{
    private readonly ILogger<ChunkingService> _logger;

    // Common CSS selectors for product/item containers
    private static readonly string[] ContainerSelectors = new[]
    {
        ".product-item",
        ".product",
        "[class*='product']",
        "article",
        ".item",
        "li"
    };

    // Common closing tags for boundary detection
    private static readonly string[] ClosingTags = new[]
    {
        "</div>",
        "</article>",
        "</li>",
        "</section>",
        "</p>"
    };

    public ChunkingService(ILogger<ChunkingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Chunk HTML content intelligently by element boundaries
    /// </summary>
    /// <param name="html">HTML content to chunk</param>
    /// <param name="chunkSize">Size of each chunk in characters (default: 15000)</param>
    /// <param name="overlap">Number of characters to overlap between chunks (default: 500)</param>
    /// <returns>List of HTML chunks</returns>
    public List<string> ChunkHtml(string html, int chunkSize = 15000, int overlap = 500)
    {
        if (string.IsNullOrWhiteSpace(html))
            return new List<string>();

        if (html.Length <= chunkSize)
            return new List<string> { html };

        try
        {
            // Try element-based chunking first (more intelligent)
            var elementChunks = TryChunkByElements(html, chunkSize);
            if (elementChunks != null && elementChunks.Count > 0)
            {
                _logger.LogInformation("Successfully chunked by elements: {ChunkCount} chunks", elementChunks.Count);
                return elementChunks;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Element-based chunking failed, falling back to character chunking");
        }

        // Fallback: Character-based chunking with tag boundary awareness
        var characterChunks = ChunkByCharacters(html, chunkSize, overlap);
        _logger.LogInformation("Chunked by characters: {ChunkCount} chunks", characterChunks.Count);
        return characterChunks;
    }

    /// <summary>
    /// Try to chunk HTML by complete elements (products, articles, etc.)
    /// </summary>
    private List<string>? TryChunkByElements(string html, int chunkSize)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Find common container elements for products/items
        HtmlNodeCollection? containers = null;

        foreach (var selector in ContainerSelectors)
        {
            containers = doc.DocumentNode.SelectNodes(selector);
            if (containers != null && containers.Count > 5)
            {
                _logger.LogInformation(
                    "Found {ContainerCount} containers with selector '{Selector}', chunking by elements",
                    containers.Count, selector);
                break;
            }
        }

        // Need at least 5 containers to justify element-based chunking
        if (containers == null || containers.Count <= 5)
            return null;

        // Group elements into chunks
        var chunks = new List<string>();
        var currentChunk = "";

        foreach (var container in containers)
        {
            var containerHtml = container.OuterHtml;

            if (currentChunk.Length + containerHtml.Length > chunkSize)
            {
                if (!string.IsNullOrEmpty(currentChunk))
                {
                    chunks.Add(currentChunk);
                }
                currentChunk = containerHtml;
            }
            else
            {
                currentChunk += containerHtml;
            }
        }

        if (!string.IsNullOrEmpty(currentChunk))
        {
            chunks.Add(currentChunk);
        }

        _logger.LogInformation("Created {ChunkCount} chunks from {ContainerCount} elements", chunks.Count, containers.Count);
        return chunks;
    }

    /// <summary>
    /// Chunk HTML by characters with tag boundary awareness
    /// </summary>
    private List<string> ChunkByCharacters(string html, int chunkSize, int overlap)
    {
        var chunks = new List<string>();
        var start = 0;

        while (start < html.Length)
        {
            var end = Math.Min(start + chunkSize, html.Length);

            // Try to end at a closing tag to avoid splitting elements
            if (end < html.Length)
            {
                var bestBreak = FindBestBreakPoint(html, end);
                if (bestBreak > start)
                {
                    end = bestBreak;
                }
            }

            var chunk = html.Substring(start, end - start);
            chunks.Add(chunk);

            // Move start position with overlap
            start = end - overlap;

            // Ensure we make progress (avoid infinite loop)
            if (start <= chunks.Count * chunkSize - overlap * chunks.Count)
            {
                start = end;
            }
        }

        return chunks;
    }

    /// <summary>
    /// Find the best break point near the target position
    /// Tries to break at closing tags to preserve element integrity
    /// </summary>
    private int FindBestBreakPoint(string html, int targetEnd)
    {
        const int searchRange = 100;
        var searchStart = Math.Max(0, targetEnd - searchRange);
        var searchEnd = Math.Min(html.Length, targetEnd + searchRange);
        var searchText = html.Substring(searchStart, searchEnd - searchStart);

        // Look for nearest closing tag before target position
        var bestBreak = targetEnd;

        foreach (var tag in ClosingTags)
        {
            var tagPos = searchText.LastIndexOf(tag, StringComparison.Ordinal);
            if (tagPos != -1)
            {
                // Adjust to absolute position and include the closing tag
                bestBreak = searchStart + tagPos + tag.Length;
                break;
            }
        }

        return bestBreak;
    }

    /// <summary>
    /// Chunk text content (non-HTML) into overlapping segments
    /// </summary>
    public List<string> ChunkText(string text, int chunkSize = 2000, int overlap = 200)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        if (text.Length <= chunkSize)
            return new List<string> { text };

        var chunks = new List<string>();
        var start = 0;

        while (start < text.Length)
        {
            var end = Math.Min(start + chunkSize, text.Length);

            // Try to break at sentence or paragraph boundaries
            if (end < text.Length)
            {
                // Look for sentence endings near the target position
                var searchStart = Math.Max(start, end - 100);
                var searchEnd = Math.Min(text.Length, end + 100);
                var searchText = text.Substring(searchStart, searchEnd - searchStart);

                var sentenceEnds = new[] { ". ", ".\n", "! ", "?\n", "? " };
                foreach (var sentenceEnd in sentenceEnds)
                {
                    var pos = searchText.LastIndexOf(sentenceEnd, StringComparison.Ordinal);
                    if (pos != -1)
                    {
                        end = searchStart + pos + sentenceEnd.Length;
                        break;
                    }
                }
            }

            var chunk = text.Substring(start, end - start).Trim();
            if (!string.IsNullOrWhiteSpace(chunk))
            {
                chunks.Add(chunk);
            }

            // Move start position with overlap
            start = end - overlap;

            // Ensure we make progress
            if (start < end - chunkSize)
            {
                start = end;
            }
        }

        return chunks;
    }
}
