using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WebCrawlerService.Domain.Enums;
using WebCrawlerService.Domain.Interfaces;
using WebCrawlerService.Domain.Models;

namespace WebCrawlerService.Infrastructure.Services;

/// <summary>
/// Generic service for mobile API-based crawling
/// Routes requests to appropriate provider-specific services
/// </summary>
public class MobileApiCrawlerService : IMobileApiCrawlerService
{
    private readonly IShopeeApiService _shopeeApiService;
    private readonly ILogger<MobileApiCrawlerService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public MobileApiCrawlerService(
        IShopeeApiService shopeeApiService,
        ILogger<MobileApiCrawlerService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _shopeeApiService = shopeeApiService;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Check if this service can handle the given URL and provider
    /// </summary>
    public async Task<bool> CanHandleUrl(string url, MobileApiProvider provider)
    {
        return provider switch
        {
            MobileApiProvider.Shopee => _shopeeApiService.IsShopeeUrl(url),
            MobileApiProvider.Lazada => IsLazadaUrl(url),
            MobileApiProvider.Tiki => IsTikiUrl(url),
            MobileApiProvider.Sendo => IsSendoUrl(url),
            MobileApiProvider.Generic => Uri.IsWellFormedUriString(url, UriKind.Absolute),
            _ => false
        };
    }

    /// <summary>
    /// Get configuration for a specific mobile API provider
    /// </summary>
    public async Task<MobileApiConfiguration> GetConfigurationAsync(MobileApiProvider provider)
    {
        return provider switch
        {
            MobileApiProvider.Shopee => new MobileApiConfiguration
            {
                Provider = MobileApiProvider.Shopee,
                BaseUrl = "https://shopee.vn",
                ApiVersion = "v4",
                RequiredHeaders = new Dictionary<string, string>
                {
                    { "User-Agent", "Shopee/2.98.21 Android/11" },
                    { "X-API-Source", "pc" },
                    { "X-Shopee-Language", "vi" }
                },
                RateLimitPerMinute = 20,
                Region = "VN"
            },
            MobileApiProvider.Lazada => new MobileApiConfiguration
            {
                Provider = MobileApiProvider.Lazada,
                BaseUrl = "https://api.lazada.vn",
                ApiVersion = "v2",
                RequiredHeaders = new Dictionary<string, string>
                {
                    { "User-Agent", "Lazada/1.0 Android" }
                },
                RateLimitPerMinute = 30,
                Region = "VN"
            },
            MobileApiProvider.Tiki => new MobileApiConfiguration
            {
                Provider = MobileApiProvider.Tiki,
                BaseUrl = "https://tiki.vn",
                ApiVersion = "v2",
                RequiredHeaders = new Dictionary<string, string>
                {
                    { "User-Agent", "Tiki/1.0 Android" }
                },
                RateLimitPerMinute = 30,
                Region = "VN"
            },
            _ => throw new NotSupportedException($"Provider {provider} is not supported")
        };
    }

    /// <summary>
    /// Execute a generic API call with the given configuration
    /// </summary>
    public async Task<T> ExecuteApiCallAsync<T>(
        string endpoint,
        Dictionary<string, string> parameters,
        MobileApiConfiguration config,
        CancellationToken cancellationToken = default)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentException("Endpoint cannot be empty", nameof(endpoint));

        _logger.LogDebug("Executing API call to {Provider}: {Endpoint}",
            config.Provider, endpoint);

        var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(30);

        // Build URL with query parameters
        var uriBuilder = new UriBuilder(config.BaseUrl + endpoint);
        if (parameters?.Any() == true)
        {
            var queryString = string.Join("&",
                parameters.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
            uriBuilder.Query = queryString;
        }

        // Create request with required headers
        using var request = new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri);
        foreach (var header in config.RequiredHeaders)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Execute request
        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("API call failed with status {StatusCode}: {Error}",
                response.StatusCode, error);
            throw new HttpRequestException(
                $"API call failed with status {response.StatusCode}: {error}");
        }

        // Deserialize response
        try
        {
            var result = await response.Content.ReadFromJsonAsync<T>(
                cancellationToken: cancellationToken);

            if (result == null)
                throw new JsonException("Failed to deserialize response");

            _logger.LogDebug("Successfully executed API call to {Provider}",
                config.Provider);

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize API response from {Provider}",
                config.Provider);
            throw;
        }
    }

    /// <summary>
    /// Check if URL is a Lazada URL
    /// </summary>
    private bool IsLazadaUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        try
        {
            var uri = new Uri(url);
            return uri.Host.Contains("lazada.vn", StringComparison.OrdinalIgnoreCase) ||
                   uri.Host.Contains("lazada.com", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Check if URL is a Tiki URL
    /// </summary>
    private bool IsTikiUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        try
        {
            var uri = new Uri(url);
            return uri.Host.Contains("tiki.vn", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Check if URL is a Sendo URL
    /// </summary>
    private bool IsSendoUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        try
        {
            var uri = new Uri(url);
            return uri.Host.Contains("sendo.vn", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
