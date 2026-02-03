using WebCrawlerService.Domain.Models;

namespace WebCrawlerService.Domain.Interfaces;

/// <summary>
/// Provides Google Custom Search powered product lookups.
/// </summary>
public interface IGoogleProductSearchService
{
    /// <summary>
    /// Search Google for products similar to the supplied descriptor.
    /// </summary>
    /// <param name="product">Known product attributes extracted from crawl.</param>
    /// <param name="maxResults">Maximum links to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<GoogleProductSearchResult>> SearchSimilarProductsAsync(
        ProductDescriptor product,
        int maxResults = 3,
        CancellationToken cancellationToken = default);
}
