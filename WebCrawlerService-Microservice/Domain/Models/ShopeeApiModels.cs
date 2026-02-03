namespace WebCrawlerService.Domain.Models;

// ==================== PRODUCT MODELS ====================

/// <summary>
/// Shopee API response wrapper for product details
/// </summary>
public class ShopeeProductResponse
{
    public ShopeeProduct? Item { get; set; }
    public int Error { get; set; }
    public string? ErrorMsg { get; set; }
}

/// <summary>
/// Shopee product details
/// </summary>
public class ShopeeProduct
{
    public long ItemId { get; set; }
    public long ShopId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Price in smallest currency unit (e.g., 100000 = 1.00 VND when divided by 100000)
    /// </summary>
    public long Price { get; set; }

    /// <summary>
    /// Original price before discount
    /// </summary>
    public long PriceBeforeDiscount { get; set; }

    public int Stock { get; set; }
    public int Sold { get; set; }

    /// <summary>
    /// Average item rating (0.0 - 5.0)
    /// </summary>
    public double ItemRating { get; set; }

    public int RatingCount { get; set; }

    /// <summary>
    /// Product image URLs
    /// </summary>
    public string[] Images { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Shop information
    /// </summary>
    public ShopeeShopInfo? ShopInfo { get; set; }

    /// <summary>
    /// Product attributes (size, color, etc.)
    /// </summary>
    public ShopeeAttribute[] Attributes { get; set; } = Array.Empty<ShopeeAttribute>();

    /// <summary>
    /// Product category ID
    /// </summary>
    public long? Catid { get; set; }

    /// <summary>
    /// Number of views
    /// </summary>
    public int? ViewCount { get; set; }

    /// <summary>
    /// Number of likes
    /// </summary>
    public int? LikeCount { get; set; }
}

/// <summary>
/// Shop information embedded in product response
/// </summary>
public class ShopeeShopInfo
{
    public long ShopId { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Shop rating (0.0 - 5.0)
    /// </summary>
    public double ShopRating { get; set; }

    public int FollowerCount { get; set; }

    /// <summary>
    /// Response rate percentage (0-100)
    /// </summary>
    public int ResponseRate { get; set; }

    /// <summary>
    /// Response time in minutes
    /// </summary>
    public int ResponseTime { get; set; }

    /// <summary>
    /// Whether shop is official
    /// </summary>
    public bool? IsOfficial { get; set; }

    /// <summary>
    /// Whether shop is verified
    /// </summary>
    public bool? IsShopeeVerified { get; set; }
}

/// <summary>
/// Product attribute (e.g., color: red, size: XL)
/// </summary>
public class ShopeeAttribute
{
    public long AttributeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

// ==================== REVIEW MODELS ====================

/// <summary>
/// Shopee API response wrapper for product reviews
/// </summary>
public class ShopeeReviewsResponse
{
    public ShopeeReview[]? Ratings { get; set; }
    public int Error { get; set; }
    public string? ErrorMsg { get; set; }
    public ShopeeRatingSummary? RatingSummary { get; set; }
}

/// <summary>
/// Individual product review
/// </summary>
public class ShopeeReview
{
    /// <summary>
    /// Reviewer username (anonymized)
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// Reviewer avatar URL
    /// </summary>
    public string? AuthorPortrait { get; set; }

    /// <summary>
    /// Rating stars (1-5)
    /// </summary>
    public int RatingStar { get; set; }

    /// <summary>
    /// Review comment text
    /// </summary>
    public string Comment { get; set; } = string.Empty;

    /// <summary>
    /// Creation time (Unix timestamp)
    /// </summary>
    public long Ctime { get; set; }

    /// <summary>
    /// Review image URLs
    /// </summary>
    public string[] Images { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Review video URLs
    /// </summary>
    public string[] Videos { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Number of likes on this review
    /// </summary>
    public int LikeCount { get; set; }

    /// <summary>
    /// Review tags (e.g., "good quality", "fast shipping")
    /// </summary>
    public string[] Tags { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Product attributes purchased (e.g., "Color: Red, Size: L")
    /// </summary>
    public string? ProductItems { get; set; }

    /// <summary>
    /// Whether this is a verified purchase
    /// </summary>
    public bool? IsAnonymous { get; set; }
}

/// <summary>
/// Summary of all reviews for a product
/// </summary>
public class ShopeeRatingSummary
{
    /// <summary>
    /// Average rating (0.0 - 5.0)
    /// </summary>
    public double AverageRating { get; set; }

    /// <summary>
    /// Total number of reviews
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Breakdown of ratings by star (1-5 stars)
    /// Key: star number, Value: count
    /// </summary>
    public Dictionary<int, int> RatingBreakdown { get; set; } = new();

    /// <summary>
    /// Number of reviews with text comments
    /// </summary>
    public int WithCommentCount { get; set; }

    /// <summary>
    /// Number of reviews with photos/videos
    /// </summary>
    public int WithMediaCount { get; set; }
}

// ==================== SEARCH MODELS ====================

/// <summary>
/// Shopee search API response
/// </summary>
public class ShopeeSearchResponse
{
    public ShopeeSearchItem[]? Items { get; set; }
    public int TotalCount { get; set; }
    public int Error { get; set; }
    public string? ErrorMsg { get; set; }
}

/// <summary>
/// Product item in search results
/// </summary>
public class ShopeeSearchItem
{
    public long ItemId { get; set; }
    public long ShopId { get; set; }
    public string Name { get; set; } = string.Empty;
    public long Price { get; set; }
    public long PriceMin { get; set; }
    public long PriceMax { get; set; }
    public string Image { get; set; } = string.Empty;
    public int Sold { get; set; }
    public double ItemRating { get; set; }
    public string ShopName { get; set; } = string.Empty;
}

// ==================== SHOP MODELS ====================

/// <summary>
/// Detailed shop information
/// </summary>
public class ShopeeShopDetails
{
    public long ShopId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Logo { get; set; } = string.Empty;
    public string Cover { get; set; } = string.Empty;
    public int FollowerCount { get; set; }
    public int ProductCount { get; set; }
    public double Rating { get; set; }
    public int ResponseRate { get; set; }
    public int ResponseTime { get; set; }
    public bool IsOfficial { get; set; }
    public bool IsShopeeVerified { get; set; }
    public long CreatedTime { get; set; }
    public string Country { get; set; } = string.Empty;
}

/// <summary>
/// Filter options for reviews
/// </summary>
public enum ReviewFilter
{
    /// <summary>
    /// All reviews
    /// </summary>
    All = 0,

    /// <summary>
    /// Reviews with text comments
    /// </summary>
    WithComments = 1,

    /// <summary>
    /// Reviews with photos or videos
    /// </summary>
    WithMedia = 2
}
