namespace WebCrawlerService.Domain.Models;

/// <summary>
/// Result from Google Custom Search relevant to a crawled product.
/// </summary>
public sealed class GoogleProductSearchResult
{
    public string ProductName { get; init; } = string.Empty;
    public string Query { get; init; } = string.Empty;
    public string? Title { get; init; }
    public string? Link { get; init; }
    public string? Snippet { get; init; }
    public string? DisplayLink { get; init; }
}
