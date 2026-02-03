using System.Text.Json;
using WebCrawlerService.Domain.Entities;
using WebCrawlerService.Domain.Enums;
using WebCrawlerService.Domain.Models;

namespace WebCrawlerService.Infrastructure.Data.SeedData;

/// <summary>
/// Seed data for Shopee crawling templates
/// Provides pre-configured templates for common Shopee crawling scenarios
/// </summary>
public static class ShopeeTemplateSeed
{
    /// <summary>
    /// Get all Shopee seed templates
    /// </summary>
    public static List<CrawlTemplate> GetShopeeTemplates()
    {
        return new List<CrawlTemplate>
        {
            GetShopeeProductTemplate(),
            GetShopeeReviewsTemplate(),
            GetShopeeSearchTemplate()
        };
    }

    /// <summary>
    /// Template for crawling Shopee product pages
    /// Extracts product details, pricing, ratings, and specifications
    /// </summary>
    private static CrawlTemplate GetShopeeProductTemplate()
    {
        var templateId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var mobileApiConfig = new MobileApiConfiguration
        {
            Provider = MobileApiProvider.Shopee,
            BaseUrl = "https://shopee.vn",
            ApiVersion = "v4",
            RequiredHeaders = new Dictionary<string, string>
            {
                { "User-Agent", "Shopee/2.98.21 Android/11" },
                { "X-API-Source", "pc" },
                { "X-Shopee-Language", "vi" },
                { "Accept", "application/json" }
            },
            RateLimitPerMinute = 20,
            RequiresSignature = false,
            RequiresCookies = false,
            RequiresProxy = false
        };

        var configJson = new
        {
            selectors = new
            {
                productName = new[] { "h1.product-title", ".product-name", "#product-name" },
                price = new[] { ".product-price", "[data-price]", ".price-text" },
                originalPrice = new[] { ".original-price", ".old-price" },
                rating = new[] { ".product-rating", "[data-rating]" },
                stock = new[] { ".stock-quantity", "[data-stock]" },
                sold = new[] { ".sold-count", "[data-sold]" },
                description = new[] { ".product-description", ".description-content" },
                images = new[] { ".product-image img", ".gallery-image" }
            },
            dynamicSelectors = new Dictionary<string, string>(),
            requiresJavaScript = false,
            scrollToBottom = false,
            waitForSelectors = new[] { ".product-title", ".product-price" },
            confidence = 0.95,
            estimatedTimeSeconds = 2
        };

        return new CrawlTemplate
        {
            Id = templateId,
            Name = "Shopee Product Page",
            Description = "Extract product information from Shopee product pages including price, rating, specifications, and reviews summary",
            DomainPattern = "*.shopee.vn/product/*,*.shopee.vn/*-i.*.*",
            Type = TemplateType.MobileApp,
            RecommendedCrawler = CrawlerType.AppSpecificApi,
            ConfigurationJson = JsonSerializer.Serialize(configJson),
            MobileApiProvider = MobileApiProvider.Shopee,
            MobileApiConfigJson = JsonSerializer.Serialize(mobileApiConfig),
            SampleUrls = new[]
            {
                "https://shopee.vn/Ch%C4%83n-cotton-%C4%91%C5%A9i-3-l%E1%BB%9Bp-1m8x2m-m%E1%BB%81m-m%E1%BB%8Bn-ch%C4%83n-%C4%91%C5%A9i-x%C6%A1-%C4%91%E1%BA%ADu-l%C3%A0nh-i.29708084.23984073012",
                "https://shopee.vn/product-example-i.123456.789012"
            },
            Version = 1,
            IsActive = true,
            IsValidated = true,
            LastTestedAt = DateTime.UtcNow,
            RateLimitDelayMs = 500,
            RequiresAuthentication = false,
            ApiEndpointPattern = "/api/v4/item/get?itemid={itemId}&shopid={shopId}",
            Tags = new[] { "shopee", "product", "ecommerce", "vietnam", "mobile-api" },
            IsSystemTemplate = true,
            IsPublic = true,
            UsageCount = 0,
            SuccessRate = 0.0,
            AverageExtractionTimeMs = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Template for crawling Shopee product reviews
    /// Extracts user reviews, ratings, comments, and review metadata
    /// </summary>
    private static CrawlTemplate GetShopeeReviewsTemplate()
    {
        var templateId = Guid.Parse("10000000-0000-0000-0000-000000000002");
        var mobileApiConfig = new MobileApiConfiguration
        {
            Provider = MobileApiProvider.Shopee,
            BaseUrl = "https://shopee.vn",
            ApiVersion = "v4",
            RequiredHeaders = new Dictionary<string, string>
            {
                { "User-Agent", "Shopee/2.98.21 Android/11" },
                { "X-API-Source", "pc" },
                { "X-Shopee-Language", "vi" },
                { "Accept", "application/json" }
            },
            RateLimitPerMinute = 20,
            RequiresSignature = false,
            RequiresCookies = false,
            RequiresProxy = false
        };

        var configJson = new
        {
            pagination = new
            {
                enabled = true,
                limitPerPage = 20,
                maxPages = 50,
                offsetParameter = "offset"
            },
            selectors = new
            {
                reviewText = new[] { ".review-comment", ".comment-text" },
                rating = new[] { ".review-rating", "[data-rating]" },
                reviewerName = new[] { ".reviewer-name", ".user-name" },
                reviewDate = new[] { ".review-date", "[data-date]" },
                helpful = new[] { ".helpful-count", "[data-helpful]" },
                images = new[] { ".review-image img" }
            },
            filters = new
            {
                all = 0,
                withComment = 1,
                withImage = 2,
                fiveStar = 5,
                fourStar = 4,
                threeStar = 3,
                twoStar = 2,
                oneStar = 1
            },
            confidence = 0.90,
            estimatedTimeSeconds = 3
        };

        return new CrawlTemplate
        {
            Id = templateId,
            Name = "Shopee Product Reviews",
            Description = "Extract user reviews and ratings from Shopee products with pagination support",
            DomainPattern = "*.shopee.vn/product/*,*.shopee.vn/*-i.*.*",
            Type = TemplateType.MobileApp,
            RecommendedCrawler = CrawlerType.AppSpecificApi,
            ConfigurationJson = JsonSerializer.Serialize(configJson),
            MobileApiProvider = MobileApiProvider.Shopee,
            MobileApiConfigJson = JsonSerializer.Serialize(mobileApiConfig),
            SampleUrls = new[]
            {
                "https://shopee.vn/Ch%C4%83n-cotton-%C4%91%C5%A9i-3-l%E1%BB%9Bp-1m8x2m-m%E1%BB%81m-m%E1%BB%8Bn-ch%C4%83n-%C4%91%C5%A9i-x%C6%A1-%C4%91%E1%BA%ADu-l%C3%A0nh-i.29708084.23984073012"
            },
            Version = 1,
            IsActive = true,
            IsValidated = true,
            LastTestedAt = DateTime.UtcNow,
            RateLimitDelayMs = 500,
            RequiresAuthentication = false,
            ApiEndpointPattern = "/api/v4/item/get_ratings?itemid={itemId}&shopid={shopId}&limit={limit}&offset={offset}",
            Tags = new[] { "shopee", "reviews", "ratings", "feedback", "mobile-api" },
            IsSystemTemplate = true,
            IsPublic = true,
            UsageCount = 0,
            SuccessRate = 0.0,
            AverageExtractionTimeMs = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Template for searching products on Shopee
    /// Searches by keyword and extracts product listings
    /// </summary>
    private static CrawlTemplate GetShopeeSearchTemplate()
    {
        var templateId = Guid.Parse("10000000-0000-0000-0000-000000000003");
        var mobileApiConfig = new MobileApiConfiguration
        {
            Provider = MobileApiProvider.Shopee,
            BaseUrl = "https://shopee.vn",
            ApiVersion = "v4",
            RequiredHeaders = new Dictionary<string, string>
            {
                { "User-Agent", "Shopee/2.98.21 Android/11" },
                { "X-API-Source", "pc" },
                { "X-Shopee-Language", "vi" },
                { "Accept", "application/json" }
            },
            RateLimitPerMinute = 20,
            RequiresSignature = false,
            RequiresCookies = false,
            RequiresProxy = false
        };

        var configJson = new
        {
            pagination = new
            {
                enabled = true,
                limitPerPage = 60,
                maxPages = 20,
                offsetParameter = "offset"
            },
            selectors = new
            {
                items = new[] { ".search-result-item", ".product-item" },
                productName = new[] { ".product-title", ".item-name" },
                price = new[] { ".product-price", "[data-price]" },
                sold = new[] { ".sold-count", "[data-sold]" },
                rating = new[] { ".product-rating" },
                shopName = new[] { ".shop-name" },
                productUrl = new[] { "a[href*='-i.']" }
            },
            sortOptions = new
            {
                relevancy = "relevancy",
                sales = "sales",
                price_asc = "price",
                price_desc = "price_desc",
                latest = "ctime"
            },
            confidence = 0.85,
            estimatedTimeSeconds = 2
        };

        return new CrawlTemplate
        {
            Id = templateId,
            Name = "Shopee Product Search",
            Description = "Search for products on Shopee by keyword with filtering and sorting options",
            DomainPattern = "*.shopee.vn/search*,*.shopee.vn/api/v4/search/*",
            Type = TemplateType.MobileApp,
            RecommendedCrawler = CrawlerType.AppSpecificApi,
            ConfigurationJson = JsonSerializer.Serialize(configJson),
            MobileApiProvider = MobileApiProvider.Shopee,
            MobileApiConfigJson = JsonSerializer.Serialize(mobileApiConfig),
            SampleUrls = new[]
            {
                "https://shopee.vn/search?keyword=laptop",
                "https://shopee.vn/search?keyword=dien%20thoai"
            },
            Version = 1,
            IsActive = true,
            IsValidated = true,
            LastTestedAt = DateTime.UtcNow,
            RateLimitDelayMs = 500,
            RequiresAuthentication = false,
            ApiEndpointPattern = "/api/v4/search/search_items?keyword={keyword}&limit={limit}&offset={offset}",
            Tags = new[] { "shopee", "search", "discovery", "listing", "mobile-api" },
            IsSystemTemplate = true,
            IsPublic = true,
            UsageCount = 0,
            SuccessRate = 0.0,
            AverageExtractionTimeMs = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
