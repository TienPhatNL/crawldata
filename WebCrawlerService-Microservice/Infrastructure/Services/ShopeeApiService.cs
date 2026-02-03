using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebCrawlerService.Domain.Interfaces;
using WebCrawlerService.Domain.Models;

namespace WebCrawlerService.Infrastructure.Services;

/// <summary>
/// Shopee-specific API exception
/// </summary>
public class ShopeeApiException : Exception
{
    public int? ErrorCode { get; }
    public string? ErrorDetails { get; }

    public ShopeeApiException(string message) : base(message)
    {
    }

    public ShopeeApiException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public ShopeeApiException(string message, int errorCode, string? errorDetails = null) : base(message)
    {
        ErrorCode = errorCode;
        ErrorDetails = errorDetails;
    }
}

/// <summary>
/// Service for interacting with Shopee mobile app APIs
/// Handles product data, reviews, search, and shop information extraction
/// </summary>
public class ShopeeApiService : IShopeeApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ShopeeApiService> _logger;
    private readonly ShopeeApiConfiguration _config;
    private readonly IProxyRotationService? _proxyService;
    private readonly SemaphoreSlim _rateLimiter;
    private DateTime _lastRequestTime = DateTime.MinValue;
    private ProxyConfiguration? _currentProxy;

    public ShopeeApiService(
        IHttpClientFactory httpClientFactory,
        IOptions<ShopeeApiConfiguration> config,
        ILogger<ShopeeApiService> logger,
        IProxyRotationService? proxyService = null)
    {
        _httpClient = httpClientFactory.CreateClient("ShopeeApi");
        _config = config.Value;
        _logger = logger;
        _proxyService = proxyService;

        // Initialize rate limiter based on config
        _rateLimiter = new SemaphoreSlim(_config.RateLimitPerMinute, _config.RateLimitPerMinute);

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);

        // Set required headers
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", _config.UserAgent);
        _httpClient.DefaultRequestHeaders.Add("X-API-Source", "pc");
        _httpClient.DefaultRequestHeaders.Add("X-Shopee-Language", _config.Language);
        _httpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        _httpClient.DefaultRequestHeaders.Add("Referer", _config.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", $"{_config.Language},en;q=0.9");

        // Add session cookies if configured
        if (!string.IsNullOrEmpty(_config.SessionCookies))
        {
            _httpClient.DefaultRequestHeaders.Add("Cookie", _config.SessionCookies);
        }
    }

    public async Task<ShopeeProduct> GetProductAsync(long itemId, long shopId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoint = $"/api/v4/item/get?itemid={itemId}&shopid={shopId}";

            _logger.LogInformation("Fetching Shopee product: itemId={ItemId}, shopId={ShopId}",
                itemId, shopId);

            var response = await ExecuteWithRetryAsync<ShopeeProductResponse>(
                endpoint, cancellationToken);

            if (response == null)
            {
                throw new ShopeeApiException("Received null response from Shopee API");
            }

            if (response.Error != 0)
            {
                throw new ShopeeApiException(
                    $"Shopee API error: {response.ErrorMsg ?? "Unknown error"}",
                    response.Error,
                    response.ErrorMsg);
            }

            if (response.Item == null)
            {
                throw new ShopeeApiException($"Product not found: itemId={itemId}, shopId={shopId}");
            }

            _logger.LogInformation("Successfully fetched product: {ProductName}", response.Item.Name);

            return response.Item;
        }
        catch (Exception ex) when (ex is not ShopeeApiException)
        {
            _logger.LogError(ex, "Error fetching Shopee product: itemId={ItemId}, shopId={ShopId}",
                itemId, shopId);
            throw new ShopeeApiException($"Failed to fetch Shopee product: {ex.Message}", ex);
        }
    }

    public async Task<ShopeeReviewsResponse> GetProductReviewsAsync(long itemId, long shopId,
        int limit = 20, int offset = 0, ReviewFilter filter = ReviewFilter.All,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate and cap limit
            limit = Math.Min(limit, 50);

            var endpoint = $"/api/v4/item/get_ratings" +
                         $"?itemid={itemId}" +
                         $"&shopid={shopId}" +
                         $"&limit={limit}" +
                         $"&offset={offset}" +
                         $"&filter={(int)filter}" +
                         $"&type=0"; // 0 = all stars, 1-5 = specific star rating

            _logger.LogInformation(
                "Fetching Shopee reviews: itemId={ItemId}, shopId={ShopId}, offset={Offset}, limit={Limit}",
                itemId, shopId, offset, limit);

            var response = await ExecuteWithRetryAsync<ShopeeReviewsResponse>(
                endpoint, cancellationToken);

            if (response == null)
            {
                throw new ShopeeApiException("Received null response from Shopee reviews API");
            }

            if (response.Error != 0)
            {
                throw new ShopeeApiException(
                    $"Shopee reviews API error: {response.ErrorMsg ?? "Unknown error"}",
                    response.Error,
                    response.ErrorMsg);
            }

            var reviewCount = response.Ratings?.Length ?? 0;
            _logger.LogInformation("Successfully fetched {Count} reviews", reviewCount);

            return response;
        }
        catch (Exception ex) when (ex is not ShopeeApiException)
        {
            _logger.LogError(ex, "Error fetching Shopee reviews: itemId={ItemId}, shopId={ShopId}",
                itemId, shopId);
            throw new ShopeeApiException($"Failed to fetch Shopee reviews: {ex.Message}", ex);
        }
    }

    public async Task<ShopeeSearchResponse> SearchProductsAsync(string keyword, int limit = 60,
        int offset = 0, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                throw new ArgumentException("Search keyword cannot be empty", nameof(keyword));
            }

            var encodedKeyword = Uri.EscapeDataString(keyword);
            var endpoint = $"/api/v4/search/search_items" +
                         $"?keyword={encodedKeyword}" +
                         $"&limit={limit}" +
                         $"&offset={offset}" +
                         $"&by=relevancy" +
                         $"&order=desc";

            _logger.LogInformation("Searching Shopee: keyword={Keyword}, limit={Limit}", keyword, limit);

            var response = await ExecuteWithRetryAsync<ShopeeSearchResponse>(
                endpoint, cancellationToken);

            if (response == null)
            {
                throw new ShopeeApiException("Received null response from Shopee search API");
            }

            if (response.Error != 0)
            {
                throw new ShopeeApiException(
                    $"Shopee search API error: {response.ErrorMsg ?? "Unknown error"}",
                    response.Error,
                    response.ErrorMsg);
            }

            _logger.LogInformation("Found {Count} search results", response.Items?.Length ?? 0);

            return response;
        }
        catch (Exception ex) when (ex is not ShopeeApiException)
        {
            _logger.LogError(ex, "Error searching Shopee: keyword={Keyword}", keyword);
            throw new ShopeeApiException($"Failed to search Shopee: {ex.Message}", ex);
        }
    }

    public async Task<ShopeeShopDetails> GetShopAsync(long shopId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoint = $"/api/v4/shop/get_shop_detail?shopid={shopId}";

            _logger.LogInformation("Fetching Shopee shop: shopId={ShopId}", shopId);

            var response = await ExecuteWithRetryAsync<ShopeeShopDetails>(
                endpoint, cancellationToken);

            if (response == null)
            {
                throw new ShopeeApiException($"Shop not found: shopId={shopId}");
            }

            _logger.LogInformation("Successfully fetched shop: {ShopName}", response.Name);

            return response;
        }
        catch (Exception ex) when (ex is not ShopeeApiException)
        {
            _logger.LogError(ex, "Error fetching Shopee shop: shopId={ShopId}", shopId);
            throw new ShopeeApiException($"Failed to fetch Shopee shop: {ex.Message}", ex);
        }
    }

    public (long shopId, long itemId) ParseProductUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL cannot be empty", nameof(url));
        }

        // Shopee product URL pattern: https://shopee.vn/...-i.{shopId}.{itemId}
        // Example: https://shopee.vn/product-name-i.29708084.23984073012
        var match = Regex.Match(url, @"-i\.(\d+)\.(\d+)(?:\?|$)");

        if (!match.Success)
        {
            throw new ArgumentException(
                $"Invalid Shopee product URL format. Expected pattern: *-i.{{shopId}}.{{itemId}}. URL: {url}",
                nameof(url));
        }

        var shopId = long.Parse(match.Groups[1].Value);
        var itemId = long.Parse(match.Groups[2].Value);

        _logger.LogDebug("Parsed Shopee URL: shopId={ShopId}, itemId={ItemId}", shopId, itemId);

        return (shopId, itemId);
    }

    public bool IsShopeeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var shopeeDomains = new[]
        {
            "shopee.vn",
            "shopee.sg",
            "shopee.co.th",
            "shopee.ph",
            "shopee.com.my",
            "shopee.co.id",
            "shopee.tw",
            "shopee.com.br"
        };

        return shopeeDomains.Any(domain =>
            url.Contains(domain, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Execute HTTP request with retry logic, rate limiting, and proxy rotation
    /// </summary>
    private async Task<T?> ExecuteWithRetryAsync<T>(string endpoint,
        CancellationToken cancellationToken) where T : class
    {
        int attempt = 0;
        Exception? lastException = null;

        while (attempt < _config.MaxRetries)
        {
            try
            {
                // Apply rate limiting
                await ApplyRateLimitAsync(cancellationToken);

                // Get proxy if enabled
                if (_config.UseProxy && _proxyService != null)
                {
                    _currentProxy = await _proxyService.GetNextProxyAsync(_config.Region, cancellationToken);
                    if (_currentProxy != null)
                    {
                        _logger.LogDebug("Using proxy: {ProxyUrl}",
                            _currentProxy.Url);
                    }
                }

                // Add request delay
                if (_config.RequestDelayMs > 0)
                {
                    await Task.Delay(_config.RequestDelayMs, cancellationToken);
                }

                // Execute request
                if (_config.EnableLogging)
                {
                    _logger.LogDebug("Shopee API Request: {Endpoint}", endpoint);
                }

                var requestStart = DateTime.UtcNow;
                var response = await _httpClient.GetAsync(endpoint, cancellationToken);
                var responseTime = (int)(DateTime.UtcNow - requestStart).TotalMilliseconds;

                // Handle different HTTP status codes
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    // Mark proxy as failed if used
                    if (_currentProxy != null && _proxyService != null)
                    {
                        await _proxyService.MarkProxyAsFailedAsync(
                            _currentProxy.Url, cancellationToken);
                    }

                    throw new HttpRequestException("Rate limit exceeded", null, HttpStatusCode.TooManyRequests);
                }

                if (response.StatusCode == HttpStatusCode.Forbidden ||
                    response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new ShopeeApiException(
                        $"Authentication failed (HTTP {(int)response.StatusCode}). " +
                        "Please check session cookies or API configuration.");
                }

                response.EnsureSuccessStatusCode();

                // Mark proxy as successful if used
                if (_currentProxy != null && _proxyService != null)
                {
                    await _proxyService.MarkProxyAsSuccessfulAsync(
                        _currentProxy.Url, responseTime, cancellationToken);
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                if (_config.EnableLogging)
                {
                    _logger.LogDebug("Shopee API Response: {Content}",
                        content.Length > 500 ? content.Substring(0, 500) + "..." : content);
                }

                var result = JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result;
            }
            catch (HttpRequestException ex) when (
                ex.StatusCode == HttpStatusCode.TooManyRequests &&
                attempt < _config.MaxRetries - 1)
            {
                attempt++;
                lastException = ex;

                var delay = TimeSpan.FromSeconds(
                    _config.RetryBaseDelaySeconds * Math.Pow(2, attempt)); // Exponential backoff

                _logger.LogWarning(
                    "Rate limited by Shopee API. Retrying in {Delay}s (attempt {Attempt}/{Max})",
                    delay.TotalSeconds, attempt + 1, _config.MaxRetries);

                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                if (attempt < _config.MaxRetries - 1 &&
                    (ex is HttpRequestException || ex is TaskCanceledException))
                {
                    attempt++;
                    lastException = ex;

                    var delay = TimeSpan.FromSeconds(_config.RetryBaseDelaySeconds * attempt);

                    _logger.LogWarning(ex,
                        "Request failed. Retrying in {Delay}s (attempt {Attempt}/{Max})",
                        delay.TotalSeconds, attempt + 1, _config.MaxRetries);

                    await Task.Delay(delay, cancellationToken);
                }
                else
                {
                    throw;
                }
            }
        }

        throw new ShopeeApiException(
            $"Failed after {_config.MaxRetries} retry attempts",
            lastException!);
    }

    /// <summary>
    /// Apply rate limiting using token bucket algorithm
    /// </summary>
    private async Task ApplyRateLimitAsync(CancellationToken cancellationToken)
    {
        await _rateLimiter.WaitAsync(cancellationToken);

        try
        {
            // Calculate time since last request
            var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
            var minInterval = TimeSpan.FromMinutes(1.0 / _config.RateLimitPerMinute);

            if (timeSinceLastRequest < minInterval)
            {
                var delay = minInterval - timeSinceLastRequest;
                await Task.Delay(delay, cancellationToken);
            }

            _lastRequestTime = DateTime.UtcNow;
        }
        finally
        {
            // Release the semaphore after a minute (token bucket refill)
            _ = Task.Delay(TimeSpan.FromMinutes(1), cancellationToken)
                .ContinueWith(_ => _rateLimiter.Release());
        }
    }
}
