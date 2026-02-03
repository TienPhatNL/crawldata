using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UserService.Infrastructure.Configuration;
using UserService.Infrastructure.Services.Models;

namespace UserService.Infrastructure.Services;

public class PayOSRelayClient : IPayOSRelayClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly PayOSSettings _settings;
    private readonly ILogger<PayOSRelayClient> _logger;

    public PayOSRelayClient(HttpClient httpClient, IOptions<PayOSSettings> options, ILogger<PayOSRelayClient> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<PayOSPaymentLinkResponse?> CreatePaymentLinkAsync(PayOSPaymentLinkRequest request, CancellationToken cancellationToken = default)
    {
        if (!_settings.EnableRelayFallback)
        {
            return null;
        }

        var endpoint = ResolveEndpoint();
        if (endpoint == null)
        {
            _logger.LogWarning("Relay fallback requested but base URL is not configured.");
            return null;
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(request, SerializerOptions), Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrWhiteSpace(_settings.RelayApiKey))
        {
            httpRequest.Headers.TryAddWithoutValidation("X-Api-Key", _settings.RelayApiKey);
        }

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Relay call returned status {StatusCode}", response.StatusCode);
            return null;
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<PayOSPaymentLinkResponse>(responseStream, SerializerOptions, cancellationToken);
    }

    private Uri? ResolveEndpoint()
    {
        var endpoint = string.IsNullOrWhiteSpace(_settings.RelayEndpoint)
            ? "/"
            : _settings.RelayEndpoint;

        if (Uri.IsWellFormedUriString(endpoint, UriKind.Absolute))
        {
            return new Uri(endpoint, UriKind.Absolute);
        }

        if (_httpClient.BaseAddress != null)
        {
            return new Uri(_httpClient.BaseAddress, endpoint.TrimStart('/'));
        }

        if (string.IsNullOrWhiteSpace(_settings.RelayBaseUrl))
        {
            return null;
        }

        var baseUri = new Uri(_settings.RelayBaseUrl, UriKind.Absolute);
        return new Uri(baseUri, endpoint.TrimStart('/'));
    }
}
