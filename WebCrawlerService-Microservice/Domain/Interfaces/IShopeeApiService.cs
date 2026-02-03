using WebCrawlerService.Domain.Models;

namespace WebCrawlerService.Domain.Interfaces;

/// <summary>
/// Service for interacting with Shopee mobile app APIs
/// </summary>
public interface IShopeeApiService
{
    /// <summary>
    /// Get detailed product information
    /// </summary>
    /// <param name="itemId">Product/item ID</param>
    /// <param name="shopId">Shop ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product details</returns>
    Task<ShopeeProduct> GetProductAsync(long itemId, long shopId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get product reviews with pagination
    /// </summary>
    /// <param name="itemId">Product/item ID</param>
    /// <param name="shopId">Shop ID</param>
    /// <param name="limit">Number of reviews per page (max 50)</param>
    /// <param name="offset">Pagination offset</param>
    /// <param name="filter">Review filter type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Reviews response with ratings</returns>
    Task<ShopeeReviewsResponse> GetProductReviewsAsync(long itemId, long shopId,
        int limit = 20, int offset = 0, ReviewFilter filter = ReviewFilter.All,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search for products by keyword
    /// </summary>
    /// <param name="keyword">Search query</param>
    /// <param name="limit">Number of results per page</param>
    /// <param name="offset">Pagination offset</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Search results</returns>
    Task<ShopeeSearchResponse> SearchProductsAsync(string keyword, int limit = 60,
        int offset = 0, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get detailed shop information
    /// </summary>
    /// <param name="shopId">Shop ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Shop details</returns>
    Task<ShopeeShopDetails> GetShopAsync(long shopId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Parse Shopee product URL to extract shop ID and item ID
    /// </summary>
    /// <param name="url">Shopee product URL</param>
    /// <returns>Tuple of (shopId, itemId)</returns>
    (long shopId, long itemId) ParseProductUrl(string url);

    /// <summary>
    /// Check if URL is a Shopee URL
    /// </summary>
    /// <param name="url">URL to check</param>
    /// <returns>True if Shopee URL</returns>
    bool IsShopeeUrl(string url);
}
