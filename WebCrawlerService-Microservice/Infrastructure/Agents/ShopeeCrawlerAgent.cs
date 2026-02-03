using System.Text.Json;
using Microsoft.Extensions.Logging;
using WebCrawlerService.Domain.Entities;
using WebCrawlerService.Domain.Enums;
using WebCrawlerService.Domain.Interfaces;
using WebCrawlerService.Domain.Models;

namespace WebCrawlerService.Infrastructure.Agents;

/// <summary>
/// Specialized crawler agent for Shopee mobile API crawling
/// Handles product pages, reviews, and search using Shopee's undocumented mobile APIs
/// </summary>
public class ShopeeCrawlerAgent : ICrawlerAgent
{
    private readonly IShopeeApiService _shopeeApiService;
    private readonly ICrawlTemplateRepository _templateRepository;
    private readonly ILogger<ShopeeCrawlerAgent> _logger;

    public ShopeeCrawlerAgent(
        IShopeeApiService shopeeApiService,
        ICrawlTemplateRepository templateRepository,
        ILogger<ShopeeCrawlerAgent> logger)
    {
        _shopeeApiService = shopeeApiService;
        _templateRepository = templateRepository;
        _logger = logger;
    }

    /// <summary>
    /// Check if this agent can handle the given crawl job
    /// </summary>
    public async Task<bool> CanHandleAsync(CrawlJob job, CancellationToken cancellationToken = default)
    {
        if (job.CrawlerType == CrawlerType.AppSpecificApi &&
            job.Urls.Any(url => _shopeeApiService.IsShopeeUrl(url)))
        {
            return true;
        }

        // Check if template indicates Shopee
        if (job.TemplateId.HasValue)
        {
            var template = await _templateRepository.GetByIdAsync(job.TemplateId.Value, cancellationToken);
            if (template?.MobileApiProvider == MobileApiProvider.Shopee)
            {
                return true;
            }
        }

        // Check if any URL is a Shopee URL
        return job.Urls.Any(url => _shopeeApiService.IsShopeeUrl(url));
    }

    /// <summary>
    /// Execute crawl job and return results
    /// </summary>
    public async Task<List<CrawlResult>> ExecuteAsync(
        CrawlJob job,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ShopeeCrawlerAgent executing job {JobId} with {UrlCount} URLs",
            job.Id, job.Urls.Length);

        var results = new List<CrawlResult>();
        var startTime = DateTime.UtcNow;

        foreach (var url in job.Urls)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Crawl job {JobId} cancelled", job.Id);
                break;
            }

            try
            {
                var result = await CrawlSingleUrlAsync(job, url, cancellationToken);
                results.Add(result);

                _logger.LogDebug("Successfully crawled {Url} for job {JobId}", url, job.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to crawl {Url} for job {JobId}", url, job.Id);

                // Create failed result
                results.Add(CreateFailedResult(job.Id, url, ex.Message));
            }
        }

        var totalTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
        _logger.LogInformation(
            "ShopeeCrawlerAgent completed job {JobId}: {SuccessCount}/{TotalCount} successful in {TimeMs}ms",
            job.Id, results.Count(r => r.IsSuccess), results.Count, totalTime);

        return results;
    }

    /// <summary>
    /// Crawl a single Shopee URL
    /// </summary>
    private async Task<CrawlResult> CrawlSingleUrlAsync(
        CrawlJob job,
        string url,
        CancellationToken cancellationToken)
    {
        if (!_shopeeApiService.IsShopeeUrl(url))
        {
            return CreateFailedResult(job.Id, url, "URL is not a valid Shopee URL");
        }

        var startTime = DateTime.UtcNow;

        try
        {
            // Parse URL to get shop and item IDs
            var (shopId, itemId) = _shopeeApiService.ParseProductUrl(url);

            // Determine what to crawl based on user prompt or template
            var shouldCrawlReviews = ShouldCrawlReviews(job);

            CrawlResult result;

            if (shouldCrawlReviews)
            {
                result = await CrawlProductWithReviewsAsync(
                    job.Id, url, itemId, shopId, cancellationToken);
            }
            else
            {
                result = await CrawlProductAsync(
                    job.Id, url, itemId, shopId, cancellationToken);
            }

            result.ResponseTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Shopee API HTTP error for {Url}: {Message}", url, ex.Message);
            return CreateFailedResult(job.Id, url, $"Shopee API error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error crawling {Url}", url);
            return CreateFailedResult(job.Id, url, $"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Crawl product information only
    /// </summary>
    private async Task<CrawlResult> CrawlProductAsync(
        Guid jobId,
        string url,
        long itemId,
        long shopId,
        CancellationToken cancellationToken)
    {
        var product = await _shopeeApiService.GetProductAsync(itemId, shopId, cancellationToken);

        var extractedData = new
        {
            productId = product.ItemId,
            productName = product.Name,
            price = product.Price / 100000.0, // Convert from Shopee's price format
            originalPrice = product.PriceBeforeDiscount > 0
                ? (double?)(product.PriceBeforeDiscount / 100000.0)
                : null,
            stock = product.Stock,
            sold = product.Sold,
            rating = product.ItemRating,
            ratingCount = product.RatingCount,
            likeCount = product.LikeCount,
            viewCount = product.ViewCount,
            description = product.Description,
            shopId = product.ShopId,
            shopInfo = product.ShopInfo,
            images = product.Images?.Select(img => $"https://cf.shopee.vn/file/{img}").ToArray(),
            categoryId = product.Catid,
            attributes = product.Attributes?.ToDictionary(
                attr => attr.Name,
                attr => attr.Value),
            url = url,
            crawledAt = DateTime.UtcNow
        };

        return new CrawlResult
        {
            Id = Guid.NewGuid(),
            CrawlJobId = jobId,
            Url = url,
            HttpStatusCode = 200,
            ContentType = "application/json",
            IsSuccess = true,
            CrawledAt = DateTime.UtcNow,
            Title = product.Name,
            Description = product.Description?.Length > 1000
                ? product.Description[..1000]
                : product.Description,
            ExtractedDataJson = JsonSerializer.Serialize(extractedData, new JsonSerializerOptions
            {
                WriteIndented = true
            }),
            TemplateId = null, // Could be set if using template
            ExtractionConfidence = 0.95, // High confidence for direct API calls
            Images = product.Images?
                .Select(img => $"https://cf.shopee.vn/file/{img}")
                .ToArray() ?? Array.Empty<string>(),
            ContentSize = JsonSerializer.Serialize(extractedData).Length
        };
    }

    /// <summary>
    /// Crawl product information with reviews
    /// </summary>
    private async Task<CrawlResult> CrawlProductWithReviewsAsync(
        Guid jobId,
        string url,
        long itemId,
        long shopId,
        CancellationToken cancellationToken)
    {
        // Get product info
        var product = await _shopeeApiService.GetProductAsync(itemId, shopId, cancellationToken);

        // Get reviews (first page)
        var reviewsResponse = await _shopeeApiService.GetProductReviewsAsync(
            itemId, shopId, limit: 20, offset: 0, cancellationToken: cancellationToken);

        var extractedData = new
        {
            product = new
            {
                productId = product.ItemId,
                productName = product.Name,
                price = product.Price / 100000.0,
                originalPrice = product.PriceBeforeDiscount > 0
                    ? (double?)(product.PriceBeforeDiscount / 100000.0)
                    : null,
                stock = product.Stock,
                sold = product.Sold,
                rating = product.ItemRating,
                ratingCount = product.RatingCount,
                description = product.Description,
                shopInfo = product.ShopInfo,
                images = product.Images?.Select(img => $"https://cf.shopee.vn/file/{img}").ToArray()
            },
            reviews = new
            {
                total = reviewsResponse.RatingSummary?.TotalCount ?? 0,
                count = reviewsResponse.Ratings?.Length ?? 0,
                summary = reviewsResponse.RatingSummary,
                items = reviewsResponse.Ratings?.Select(r => new
                {
                    rating = r.RatingStar,
                    comment = r.Comment,
                    author = r.Author,
                    createdAt = DateTimeOffset.FromUnixTimeSeconds(r.Ctime).DateTime,
                    likes = r.LikeCount,
                    images = r.Images?.Select(img => $"https://cf.shopee.vn/file/{img}").ToArray()
                }).ToArray()
            },
            url = url,
            crawledAt = DateTime.UtcNow
        };

        return new CrawlResult
        {
            Id = Guid.NewGuid(),
            CrawlJobId = jobId,
            Url = url,
            HttpStatusCode = 200,
            ContentType = "application/json",
            IsSuccess = true,
            CrawledAt = DateTime.UtcNow,
            Title = product.Name,
            Description = $"{product.Name} with {reviewsResponse.RatingSummary?.TotalCount ?? 0} reviews",
            ExtractedDataJson = JsonSerializer.Serialize(extractedData, new JsonSerializerOptions
            {
                WriteIndented = true
            }),
            ExtractionConfidence = 0.95,
            Images = product.Images?
                .Select(img => $"https://cf.shopee.vn/file/{img}")
                .ToArray() ?? Array.Empty<string>(),
            ContentSize = JsonSerializer.Serialize(extractedData).Length
        };
    }

    /// <summary>
    /// Determine if reviews should be crawled based on job configuration
    /// </summary>
    private bool ShouldCrawlReviews(CrawlJob job)
    {
        if (string.IsNullOrWhiteSpace(job.UserPrompt))
            return false;

        var promptLower = job.UserPrompt.ToLower();
        return promptLower.Contains("review") ||
               promptLower.Contains("rating") ||
               promptLower.Contains("feedback") ||
               promptLower.Contains("comment");
    }

    /// <summary>
    /// Create a failed crawl result
    /// </summary>
    private CrawlResult CreateFailedResult(Guid jobId, string url, string errorMessage)
    {
        return new CrawlResult
        {
            Id = Guid.NewGuid(),
            CrawlJobId = jobId,
            Url = url,
            HttpStatusCode = 0,
            IsSuccess = false,
            CrawledAt = DateTime.UtcNow,
            ErrorMessage = errorMessage,
            ExtractionConfidence = 0.0
        };
    }
}
