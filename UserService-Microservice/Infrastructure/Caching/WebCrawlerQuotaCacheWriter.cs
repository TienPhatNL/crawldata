using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using StackExchange.Redis;
using UserService.Domain.Interfaces;

namespace UserService.Infrastructure.Caching;

public class WebCrawlerQuotaCacheWriter : IWebCrawlerQuotaCacheWriter
{
    private const string CacheKeyPrefix = "quota:user:";
    private const string InstanceName = "WebCrawler_";
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(60);
    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer _redis;
    private readonly JsonSerializerOptions _jsonOptions;

    public WebCrawlerQuotaCacheWriter(IConnectionMultiplexer redis)
    {
        _cache = new RedisCache(new RedisCacheOptions
        {
            InstanceName = InstanceName,
            ConnectionMultiplexerFactory = () => Task.FromResult(redis)
        });
        _redis = redis;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task SetQuotaAsync(
        Guid userId,
        int remainingQuota,
        int totalQuota,
        string planType,
        DateTime? resetDate,
        DateTime lastUpdated,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new WebCrawlerQuotaCachePayload
        {
            RemainingQuota = remainingQuota,
            TotalQuota = totalQuota,
            PlanType = planType,
            ResetDate = resetDate,
            LastUpdated = lastUpdated
        };

        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? DefaultExpiry
        };

        var cacheKey = $"{CacheKeyPrefix}{userId}";
        try
        {
            await _cache.SetStringAsync(cacheKey, json, options, cancellationToken);
        }
        catch (RedisServerException ex) when (ex.Message.StartsWith("WRONGTYPE", StringComparison.OrdinalIgnoreCase))
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync($"{InstanceName}{cacheKey}");
            await _cache.SetStringAsync(cacheKey, json, options, cancellationToken);
        }
    }

    private sealed class WebCrawlerQuotaCachePayload
    {
        public int RemainingQuota { get; set; }
        public int TotalQuota { get; set; }
        public string PlanType { get; set; } = string.Empty;
        public DateTime? ResetDate { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}

internal sealed class NoopWebCrawlerQuotaCacheWriter : IWebCrawlerQuotaCacheWriter
{
    public Task SetQuotaAsync(
        Guid userId,
        int remainingQuota,
        int totalQuota,
        string planType,
        DateTime? resetDate,
        DateTime lastUpdated,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
