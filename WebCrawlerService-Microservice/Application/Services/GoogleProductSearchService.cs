using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebCrawlerService.Application.Configuration;
using WebCrawlerService.Domain.Interfaces;
using WebCrawlerService.Domain.Models;

namespace WebCrawlerService.Application.Services;

/// <summary>
/// Google Custom Search implementation for product discovery.
/// </summary>
public sealed class GoogleProductSearchService : IGoogleProductSearchService
{
    private readonly HttpClient _httpClient;
    private readonly GoogleSearchOptions _options;
    private readonly ILogger<GoogleProductSearchService> _logger;

    public GoogleProductSearchService(
        HttpClient httpClient,
        IOptions<GoogleSearchOptions> options,
        ILogger<GoogleProductSearchService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<GoogleProductSearchResult>> SearchSimilarProductsAsync(
        ProductDescriptor product,
        int maxResults = 3,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(product.Name))
        {
            return Array.Empty<GoogleProductSearchResult>();
        }

        var max = Math.Clamp(maxResults, 1, _options.MaxResultsPerQuery);
        var query = BuildQuery(product);
        var requestUri = BuildRequestUri(query, max);

        try
        {
            using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Google search failed for query {Query}. Status: {Status}", query, response.StatusCode);
                return Array.Empty<GoogleProductSearchResult>();
            }

            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(contentStream, cancellationToken: cancellationToken);

            if (!json.RootElement.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<GoogleProductSearchResult>();
            }

            var results = new List<GoogleProductSearchResult>();
            foreach (var item in items.EnumerateArray())
            {
                results.Add(new GoogleProductSearchResult
                {
                    ProductName = product.Name,
                    Query = query,
                    Title = item.TryGetProperty("title", out var title) ? title.GetString() : null,
                    Link = item.TryGetProperty("link", out var link) ? link.GetString() : null,
                    Snippet = item.TryGetProperty("snippet", out var snippet) ? snippet.GetString() : null,
                    DisplayLink = item.TryGetProperty("displayLink", out var display) ? display.GetString() : null
                });
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Google Custom Search for query {Query}", query);
            return Array.Empty<GoogleProductSearchResult>();
        }
    }

    private string BuildQuery(ProductDescriptor product)
    {
        var sb = new StringBuilder(product.Name);

        if (!string.IsNullOrWhiteSpace(product.Brand) &&
            !product.Name.Contains(product.Brand, StringComparison.OrdinalIgnoreCase))
        {
            sb.Append(' ').Append(product.Brand);
        }

        if (product.Price.HasValue)
        {
            var formatted = product.Price.Value.ToString("0", CultureInfo.InvariantCulture);
            sb.Append(' ').Append(formatted);
            if (!string.IsNullOrWhiteSpace(product.Currency))
            {
                sb.Append(' ').Append(product.Currency);
            }
        }

        if (!string.IsNullOrWhiteSpace(product.AdditionalNotes))
        {
            sb.Append(' ').Append(product.AdditionalNotes);
        }

        sb.Append(" mua ở đâu");
        return sb.ToString().Trim();
    }

    private string BuildRequestUri(string query, int maxResults)
    {
        var builder = new StringBuilder(_options.BaseUrl);
        var separator = _options.BaseUrl.Contains('?') ? '&' : '?';
        builder.Append(separator)
               .Append("key=").Append(Uri.EscapeDataString(_options.ApiKey))
               .Append("&cx=").Append(Uri.EscapeDataString(_options.SearchEngineId))
               .Append("&num=").Append(maxResults)
               .Append("&q=").Append(Uri.EscapeDataString(query));

        if (!string.IsNullOrWhiteSpace(_options.DefaultCountry))
        {
            builder.Append("&gl=").Append(Uri.EscapeDataString(_options.DefaultCountry));
        }

        if (!string.IsNullOrWhiteSpace(_options.DefaultLanguage))
        {
            builder.Append("&lr=").Append(Uri.EscapeDataString(_options.DefaultLanguage));
        }

        return builder.ToString();
    }
}
