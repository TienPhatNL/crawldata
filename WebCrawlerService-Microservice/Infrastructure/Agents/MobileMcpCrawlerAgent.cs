using Microsoft.Extensions.Logging;
using WebCrawlerService.Domain.Entities;
using WebCrawlerService.Domain.Enums;
using WebCrawlerService.Domain.Interfaces;
using WebCrawlerService.Domain.Models;

namespace WebCrawlerService.Infrastructure.Agents;

/// <summary>
/// Crawler agent that uses MCP + LLM for extracting data from mobile apps
/// No CSS selectors required - uses semantic understanding
/// </summary>
public class MobileMcpCrawlerAgent : ICrawlerAgent
{
    private readonly IMcpClientService _mcpClient;
    private readonly ILlmExtractionService _llmService;
    private readonly ILogger<MobileMcpCrawlerAgent> _logger;

    // Constants for timing
    private const int AppLoadDelayMs = 3000;
    private const int PageLoadDelayMs = 3000;
    private const int RateLimitDelayMs = 2000;

    // Predefined extraction schemas for different screen types
    private static readonly Dictionary<string, ExtractionSchema> ExtractionSchemas = new()
    {
        ["shopee_product_detail"] = new ExtractionSchema
        {
            Name = "shopee_product_detail",
            Description = "Extract product details from Shopee product page",
            Fields = new Dictionary<string, FieldSchema>
            {
                ["product_name"] = new() { Type = "string", Description = "Full product title/name", Required = true },
                ["price"] = new() { Type = "number", Description = "Current price in VND (numeric only)", Required = true, Example = "299000" },
                ["original_price"] = new() { Type = "number", Description = "Original price before discount (if shown)", Required = false },
                ["discount_percentage"] = new() { Type = "number", Description = "Discount percentage (if shown)", Required = false },
                ["rating"] = new() { Type = "number", Description = "Average rating (0-5)", Required = false },
                ["review_count"] = new() { Type = "number", Description = "Total number of reviews", Required = false },
                ["sold_count"] = new() { Type = "number", Description = "Number of items sold", Required = false },
                ["seller_name"] = new() { Type = "string", Description = "Shop/seller name", Required = false },
                ["stock_available"] = new() { Type = "boolean", Description = "Whether product is in stock", Required = false },
                ["product_description"] = new() { Type = "string", Description = "Short product description (first 200 chars)", Required = false }
            }
        },
        ["shopee_reviews"] = new ExtractionSchema
        {
            Name = "shopee_reviews",
            Description = "Extract list of product reviews",
            IsArray = true,
            Fields = new Dictionary<string, FieldSchema>
            {
                ["reviewer_name"] = new() { Type = "string", Description = "Name of reviewer", Required = false },
                ["rating"] = new() { Type = "number", Description = "Star rating (1-5)", Required = true },
                ["review_text"] = new() { Type = "string", Description = "Review content/text", Required = true },
                ["review_date"] = new() { Type = "string", Description = "Date of review", Required = false },
                ["helpful_count"] = new() { Type = "number", Description = "Number of helpful votes", Required = false }
            }
        }
    };

    public MobileMcpCrawlerAgent(
        IMcpClientService mcpClient,
        ILlmExtractionService llmService,
        ILogger<MobileMcpCrawlerAgent> logger)
    {
        _mcpClient = mcpClient;
        _llmService = llmService;
        _logger = logger;
    }

    public async Task<List<CrawlResult>> ExecuteAsync(CrawlJob job, CancellationToken cancellationToken = default)
    {
        var results = new List<CrawlResult>();

        try
        {
            _logger.LogInformation("Starting mobile MCP crawl job {JobId} for {Count} URLs", job.Id, job.Urls.Length);

            // Connect to Android device
            var connected = await _mcpClient.ConnectAsync(cancellationToken: cancellationToken);
            if (!connected)
            {
                _logger.LogError("Failed to connect to Android device");
                return results;
            }

            // Determine which app to use based on URL
            var appIdentifier = DetermineAppIdentifier(job.Urls.FirstOrDefault() ?? string.Empty);

            // Open app
            var appOpened = await _mcpClient.OpenAppAsync(appIdentifier, cancellationToken: cancellationToken);
            if (!appOpened)
            {
                _logger.LogError("Failed to open app {App}", appIdentifier);
                return results;
            }

            // Wait for app to load
            await Task.Delay(AppLoadDelayMs, cancellationToken);

            // Process each URL
            foreach (var url in job.Urls)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Crawl job cancelled");
                    break;
                }

                try
                {
                    var result = await CrawlUrlAsync(url, job, cancellationToken);
                    results.Add(result);

                    _logger.LogInformation("Successfully crawled {Url}", url);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error crawling {Url}", url);

                    results.Add(new CrawlResult
                    {
                        CrawlJobId = job.Id,
                        Url = url,
                        IsSuccess = false,
                        ErrorMessage = ex.Message,
                        CrawledAt = DateTime.UtcNow
                    });
                }

                // Rate limiting between requests
                if (Array.IndexOf(job.Urls, url) < job.Urls.Length - 1)
                {
                    await Task.Delay(RateLimitDelayMs, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error in MCP crawl job");
        }
        finally
        {
            // Cleanup
            await _mcpClient.DisconnectAsync(cancellationToken);
        }

        return results;
    }

    private async Task<CrawlResult> CrawlUrlAsync(string url, CrawlJob job, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Crawling URL: {Url}", url);

        // Navigate to URL (could be deep link or need to search)
        var navigated = await NavigateToUrlAsync(url, cancellationToken);

        if (!navigated)
        {
            return new CrawlResult
            {
                CrawlJobId = job.Id,
                Url = url,
                IsSuccess = false,
                ErrorMessage = "Failed to navigate to URL",
                CrawledAt = DateTime.UtcNow
            };
        }

        // Wait for page to load
        await Task.Delay(PageLoadDelayMs, cancellationToken);

        // Get current screen state
        var screenState = await _mcpClient.GetScreenStateAsync(cancellationToken);

        // Determine what type of screen we're on
        var screenType = await DetectScreenTypeAsync(screenState, url, cancellationToken);

        _logger.LogInformation("Detected screen type: {ScreenType}", screenType);

        // Extract data based on screen type
        var extractionResult = await ExtractDataAsync(screenState, screenType, cancellationToken);

        if (!extractionResult.Success)
        {
            return new CrawlResult
            {
                CrawlJobId = job.Id,
                Url = url,
                IsSuccess = false,
                ErrorMessage = extractionResult.ErrorMessage ?? "Extraction failed",
                CrawledAt = DateTime.UtcNow
            };
        }

        // If job requires reviews and we're on product page, navigate to reviews
        List<object>? reviews = null;
        var config = ParseConfiguration(job.ConfigurationJson);
        if (config.GetValueOrDefault("crawl_reviews") == "true" &&
            screenType == "shopee_product_detail")
        {
            reviews = await CrawlReviewsAsync(cancellationToken);
        }

        // Convert JsonDocument to dictionary for storage
        var data = JsonToDictionary(extractionResult.Data);

        if (reviews != null)
        {
            data["reviews"] = reviews;
        }

        // Add metadata to data
        data["_metadata"] = new Dictionary<string, object>
        {
            ["screen_type"] = screenType,
            ["extraction_model"] = extractionResult.ModelUsed,
            ["extraction_confidence"] = extractionResult.Confidence,
            ["extraction_duration_ms"] = extractionResult.Duration.TotalMilliseconds,
            ["tokens_used"] = extractionResult.TokensUsed
        };

        return new CrawlResult
        {
            CrawlJobId = job.Id,
            Url = url,
            IsSuccess = true,
            Content = System.Text.Json.JsonSerializer.Serialize(data),
            ContentType = "application/json",
            HttpStatusCode = 200,
            CrawledAt = DateTime.UtcNow,
            ExtractionConfidence = extractionResult.Confidence,
            LlmCost = extractionResult.EstimatedCost
        };
    }

    private async Task<bool> NavigateToUrlAsync(string url, CancellationToken cancellationToken)
    {
        // For Shopee, try to use deep link first
        if (url.Contains("shopee.vn"))
        {
            // Convert web URL to deep link
            var deepLink = ConvertToDeepLink(url);

            if (!string.IsNullOrEmpty(deepLink))
            {
                _logger.LogDebug("Attempting deep link: {DeepLink}", deepLink);

                var opened = await _mcpClient.OpenAppAsync(deepLink, isDeepLink: true, cancellationToken);

                if (opened)
                {
                    return true;
                }
            }
        }

        // Fallback: Use in-app search or manual navigation
        _logger.LogWarning("Deep link failed, falling back to manual navigation");

        // This would require more complex logic:
        // 1. Find search box
        // 2. Extract product name from URL
        // 3. Search for product
        // 4. Click first result

        // For now, return false - this is an area for future enhancement
        return false;
    }

    private async Task<string> DetectScreenTypeAsync(ScreenState screenState, string url, CancellationToken cancellationToken)
    {
        // First try simple heuristics based on visible text
        var visibleText = string.Join(" ", screenState.VisibleText).ToLowerInvariant();

        if (visibleText.Contains("đánh giá") || visibleText.Contains("reviews"))
        {
            if (visibleText.Contains("sao") || visibleText.Contains("stars"))
            {
                return "shopee_reviews";
            }
        }

        if (visibleText.Contains("mua ngay") || visibleText.Contains("thêm vào giỏ") ||
            visibleText.Contains("buy now") || visibleText.Contains("add to cart"))
        {
            return "shopee_product_detail";
        }

        // If heuristics fail, use LLM to validate
        var isProductPage = await _llmService.ValidateScreenTypeAsync(
            screenState,
            "product_detail_page",
            cancellationToken);

        return isProductPage ? "shopee_product_detail" : "unknown";
    }

    private async Task<ExtractionResult> ExtractDataAsync(
        ScreenState screenState,
        string screenType,
        CancellationToken cancellationToken)
    {
        if (!ExtractionSchemas.TryGetValue(screenType, out var schema))
        {
            _logger.LogWarning("No extraction schema for screen type: {ScreenType}", screenType);

            // Use generic extraction
            return await _llmService.ExtractWithPromptAsync(
                screenState,
                "Extract all visible product information as JSON",
                cancellationToken);
        }

        return await _llmService.ExtractAsync(screenState, schema, cancellationToken);
    }

    private async Task<List<object>?> CrawlReviewsAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Navigating to reviews section");

            // Use LLM to determine how to navigate to reviews
            var currentState = await _mcpClient.GetScreenStateAsync(cancellationToken);
            var action = await _llmService.DetermineNextActionAsync(
                currentState,
                "navigate to product reviews section",
                cancellationToken);

            // Parse and execute action
            var actionParts = action.Split(':');
            if (actionParts.Length == 2)
            {
                var actionType = actionParts[0];
                var target = actionParts[1];

                if (actionType == "tap")
                {
                    // Find element with matching text and tap it
                    // This is simplified - in production, would need more robust element finding
                    await _mcpClient.ScrollAsync("down", 300, cancellationToken);
                    await Task.Delay(1000, cancellationToken);

                    // For now, just scroll down to find reviews
                    await _mcpClient.ScrollAsync("down", 500, cancellationToken);
                    await Task.Delay(2000, cancellationToken);
                }
            }

            // Extract reviews
            var reviewsState = await _mcpClient.GetScreenStateAsync(cancellationToken);
            var schema = ExtractionSchemas["shopee_reviews"];
            var result = await _llmService.ExtractAsync(reviewsState, schema, cancellationToken);

            if (result.Success)
            {
                var reviewsData = JsonToDictionary(result.Data);
                if (reviewsData.TryGetValue("data", out var dataValue) && dataValue is List<object> reviews)
                {
                    return reviews;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error crawling reviews");
        }

        return null;
    }

    private string DetermineAppIdentifier(string url)
    {
        if (url.Contains("shopee.vn") || url.Contains("shopee.com"))
            return "shopee";

        if (url.Contains("lazada.vn") || url.Contains("lazada.com"))
            return "lazada";

        if (url.Contains("tiki.vn"))
            return "tiki";

        if (url.Contains("sendo.vn"))
            return "sendo";

        return "shopee"; // Default
    }

    private string ConvertToDeepLink(string url)
    {
        // Shopee deep link format: shopee://product/{shop_id}/{product_id}
        // Web URL format: https://shopee.vn/product-name-i.{shop_id}.{product_id}

        try
        {
            var match = System.Text.RegularExpressions.Regex.Match(url, @"\.(\d+)\.(\d+)");
            if (match.Success)
            {
                var shopId = match.Groups[1].Value;
                var productId = match.Groups[2].Value;
                return $"shopee://product/{shopId}/{productId}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to convert URL to deep link: {Url}", url);
        }

        return string.Empty;
    }

    private Dictionary<string, string> ParseConfiguration(string json)
    {
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                   ?? new Dictionary<string, string>();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }

    private Dictionary<string, object> JsonToDictionary(System.Text.Json.JsonDocument jsonDoc)
    {
        var result = new Dictionary<string, object>();

        foreach (var property in jsonDoc.RootElement.EnumerateObject())
        {
            result[property.Name] = JsonElementToObject(property.Value);
        }

        return result;
    }

    private object JsonElementToObject(System.Text.Json.JsonElement element)
    {
        return element.ValueKind switch
        {
            System.Text.Json.JsonValueKind.String => element.GetString() ?? string.Empty,
            System.Text.Json.JsonValueKind.Number => element.GetDouble(),
            System.Text.Json.JsonValueKind.True => true,
            System.Text.Json.JsonValueKind.False => false,
            System.Text.Json.JsonValueKind.Array => element.EnumerateArray().Select(JsonElementToObject).ToList(),
            System.Text.Json.JsonValueKind.Object => JsonToDictionary(System.Text.Json.JsonDocument.Parse(element.GetRawText())),
            _ => element.GetRawText()
        };
    }
}
