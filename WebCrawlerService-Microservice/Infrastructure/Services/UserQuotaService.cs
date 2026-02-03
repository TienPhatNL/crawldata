using System.Net.Http.Headers;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using WebCrawlerService.Domain.Events;
using WebCrawlerService.Domain.Interfaces;
using WebCrawlerService.Domain.Models;
using WebCrawlerService.Infrastructure.Messaging;

namespace WebCrawlerService.Infrastructure.Services;

/// <summary>
/// Redis-cached quota service with event-driven updates
/// </summary>
public class UserQuotaService : IUserQuotaService
{
    private readonly ICacheService _cache;
    private readonly IEventPublisher _eventPublisher;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IProducer<string, string> _producer;
    private readonly KafkaSettings _kafkaSettings;
    private readonly ILogger<UserQuotaService> _logger;
    private const string QUOTA_KEY_PREFIX = "quota:user:";
    private const int CACHE_EXPIRY_MINUTES = 60;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public UserQuotaService(
        ICacheService cache,
        IEventPublisher eventPublisher,
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        IProducer<string, string> producer,
        IOptions<KafkaSettings> kafkaOptions,
        ILogger<UserQuotaService> logger)
    {
        _cache = cache;
        _eventPublisher = eventPublisher;
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _producer = producer;
        _kafkaSettings = kafkaOptions.Value;
        _logger = logger;
    }

    public async Task<bool> CheckQuotaAsync(Guid userId, int requiredUnits = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            if (requiredUnits <= 0)
            {
                return true;
            }

            var cachedQuota = await GetOrFetchQuotaAsync(userId, cancellationToken);

            if (cachedQuota != null)
            {
                var hasQuota = cachedQuota.RemainingQuota >= requiredUnits;
                _logger.LogDebug(
                    "Quota check for user {UserId}: {RemainingQuota} remaining, required {Required}",
                    userId,
                    cachedQuota.RemainingQuota,
                    requiredUnits);
                return hasQuota;
            }

            // Cache miss - return false and let user refresh quota via re-login
            _logger.LogWarning("Quota not in cache for user {UserId}. User may need to re-login.", userId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking quota for user {UserId}", userId);
            return false;
        }
    }

    public async Task DeductQuotaAsync(Guid userId, int units, Guid? jobId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (units <= 0)
            {
                return;
            }

            var cacheKey = $"{QUOTA_KEY_PREFIX}{userId}";
            var cachedQuota = await _cache.GetAsync<UserQuotaCache>(cacheKey, cancellationToken);

            if (cachedQuota != null && cachedQuota.RemainingQuota > 0)
            {
                // Optimistic decrement in cache
                cachedQuota.RemainingQuota = Math.Max(0, cachedQuota.RemainingQuota - units);
                cachedQuota.LastUpdated = DateTime.UtcNow;
                await _cache.SetAsync(cacheKey, cachedQuota, TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES), cancellationToken);
                
                _logger.LogInformation(
                    "Quota decremented in cache for user {UserId}. New remaining: {RemainingQuota}",
                    userId,
                    cachedQuota.RemainingQuota);
            }

            // Publish event for eventual consistency with UserService
            await _eventPublisher.PublishAsync(new QuotaDeductedEvent
            {
                UserId = userId,
                Amount = units,
                Timestamp = DateTime.UtcNow,
                Source = "WebCrawlerService",
                JobId = jobId
            }, cancellationToken);

            await PublishQuotaUsageAsync(userId, jobId, units, cancellationToken);

            _logger.LogDebug("Published quota usage for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deducting quota for user {UserId}", userId);
        }
    }

    public async Task<int> GetRemainingQuotaAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var cachedQuota = await GetOrFetchQuotaAsync(userId, cancellationToken);
            return cachedQuota?.RemainingQuota ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quota for user {UserId}", userId);
            return 0;
        }
    }

    public async Task<UserQuotaInfo?> GetQuotaInfoAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var cachedQuota = await GetOrFetchQuotaAsync(userId, cancellationToken);
            if (cachedQuota == null)
            {
                return null;
            }

            return new UserQuotaInfo
            {
                UserId = userId,
                TotalQuota = cachedQuota.TotalQuota,
                RemainingQuota = cachedQuota.RemainingQuota,
                PlanType = cachedQuota.PlanType,
                ResetDate = cachedQuota.ResetDate,
                LastUpdated = cachedQuota.LastUpdated
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quota info for user {UserId}", userId);
            return null;
        }
    }

    private async Task<UserQuotaCache?> GetOrFetchQuotaAsync(Guid userId, CancellationToken cancellationToken)
    {
        var cacheKey = $"{QUOTA_KEY_PREFIX}{userId}";
        UserQuotaCache? cachedQuota = null;
        try
        {
            cachedQuota = await _cache.GetAsync<UserQuotaCache>(cacheKey, cancellationToken);
        }
        catch (RedisServerException ex) when (ex.Message.StartsWith("WRONGTYPE", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(ex, "Quota cache key has wrong type. Removing key {CacheKey}.", cacheKey);
            await _cache.RemoveAsync(cacheKey, cancellationToken);
        }

        if (cachedQuota != null)
        {
            return cachedQuota;
        }

        var accessToken = GetAccessToken();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        var fetchedQuota = await FetchQuotaFromUserServiceAsync(userId, accessToken, cancellationToken);
        if (fetchedQuota == null)
        {
            return null;
        }

        await _cache.SetAsync(cacheKey, fetchedQuota, TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES), cancellationToken);
        return fetchedQuota;
    }

    private async Task<UserQuotaCache?> FetchQuotaFromUserServiceAsync(Guid userId, string accessToken, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("UserService");
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/user/quota");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "UserService quota fetch failed for user {UserId}. Status: {StatusCode}",
                    userId,
                    response.StatusCode);
                return null;
            }

            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseQuotaResponse(payload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch quota from UserService for user {UserId}", userId);
            return null;
        }
    }

    private UserQuotaCache? ParseQuotaResponse(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        if (!document.RootElement.TryGetProperty("data", out var data))
        {
            return null;
        }

        if (!data.TryGetProperty("quotaLimit", out var limitElement) ||
            limitElement.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        var quotaLimit = Math.Max(0, limitElement.GetInt32());
        var remaining = 0;

        if (data.TryGetProperty("quotaRemaining", out var remainingElement) &&
            remainingElement.ValueKind == JsonValueKind.Number)
        {
            remaining = remainingElement.GetInt32();
        }
        else if (data.TryGetProperty("quotaUsed", out var usedElement) &&
                 usedElement.ValueKind == JsonValueKind.Number)
        {
            remaining = quotaLimit - usedElement.GetInt32();
        }

        remaining = Math.Max(0, remaining);
        if (quotaLimit > 0 && remaining > quotaLimit)
        {
            remaining = quotaLimit;
        }

        var planType = string.Empty;
        if (data.TryGetProperty("subscriptionTier", out var planElement) &&
            planElement.ValueKind == JsonValueKind.String)
        {
            planType = planElement.GetString() ?? string.Empty;
        }

        DateTime? resetDate = null;
        if (data.TryGetProperty("quotaResetDate", out var resetElement) &&
            resetElement.ValueKind == JsonValueKind.String &&
            DateTime.TryParse(resetElement.GetString(), out var parsedReset))
        {
            resetDate = parsedReset;
        }

        return new UserQuotaCache
        {
            RemainingQuota = remaining,
            TotalQuota = quotaLimit,
            PlanType = planType,
            ResetDate = resetDate,
            LastUpdated = DateTime.UtcNow
        };
    }

    private async Task PublishQuotaUsageAsync(Guid userId, Guid? jobId, int units, CancellationToken cancellationToken)
    {
        try
        {
            var correlationId = jobId?.ToString() ?? Guid.NewGuid().ToString();
            var quotaEvent = new
            {
                userId,
                jobId = jobId?.ToString(),
                unitsConsumed = units,
                correlationId,
                occurredAt = DateTime.UtcNow,
                source = "web-crawler-service"
            };

            var message = new Message<string, string>
            {
                Key = userId.ToString(),
                Value = JsonSerializer.Serialize(quotaEvent, JsonOptions)
            };

            var topic = string.IsNullOrWhiteSpace(_kafkaSettings.QuotaUsageTopic)
                ? "crawler.quota.usage"
                : _kafkaSettings.QuotaUsageTopic;

            await _producer.ProduceAsync(topic, message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish quota usage for user {UserId}", userId);
        }
    }

    private string? GetAccessToken()
    {
        var header = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header))
        {
            return null;
        }

        const string bearerPrefix = "Bearer ";
        if (header.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return header[bearerPrefix.Length..].Trim();
        }

        return header;
    }

}

/// <summary>
/// Cached user quota data
/// </summary>
public class UserQuotaCache
{
    public int RemainingQuota { get; set; }
    public int TotalQuota { get; set; }
    public string PlanType { get; set; } = string.Empty;
    public DateTime? ResetDate { get; set; }
    public DateTime LastUpdated { get; set; }
}
