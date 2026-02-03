using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UserService.Domain.Common;
using UserService.Domain.Interfaces;

namespace UserService.Infrastructure.Caching;

public class RedisPaymentConfirmationTokenStore : IPaymentConfirmationTokenStore
{
    private const string CacheScope = "payment-confirm";

    private readonly IDistributedCache _cache;
    private readonly RedisCacheSettings _settings;
    private readonly ILogger<RedisPaymentConfirmationTokenStore> _logger;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public RedisPaymentConfirmationTokenStore(
        IDistributedCache cache,
        IOptions<RedisCacheSettings> settings,
        ILogger<RedisPaymentConfirmationTokenStore> logger)
    {
        _cache = cache;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task StoreTokenAsync(string orderCode, PaymentConfirmationTokenInfo info, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(orderCode))
        {
            throw new ArgumentException("Order code is required", nameof(orderCode));
        }

        if (ttl <= TimeSpan.Zero)
        {
            ttl = TimeSpan.FromMinutes(Math.Max(1, _settings.DefaultExpirationMinutes));
        }

        var key = BuildKey(orderCode);
        var payload = JsonSerializer.Serialize(info, _serializerOptions);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        };

        await _cache.SetStringAsync(key, payload, options, cancellationToken);
        _logger.LogDebug("Stored confirmation token for order {OrderCode} with TTL {Ttl}", orderCode, ttl);
    }

    public async Task<PaymentConfirmationTokenInfo?> GetTokenAsync(string orderCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(orderCode))
        {
            return null;
        }

        var key = BuildKey(orderCode);
        var payload = await _cache.GetStringAsync(key, cancellationToken);
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<PaymentConfirmationTokenInfo>(payload, _serializerOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize payment confirmation token for order {OrderCode}", orderCode);
            await _cache.RemoveAsync(key, cancellationToken);
            return null;
        }
    }

    public Task RemoveTokenAsync(string orderCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(orderCode))
        {
            return Task.CompletedTask;
        }

        var key = BuildKey(orderCode);
        return _cache.RemoveAsync(key, cancellationToken);
    }

    private string BuildKey(string orderCode)
    {
        var trimmed = orderCode.Trim();
        return string.IsNullOrWhiteSpace(_settings.KeyPrefix)
            ? $"{CacheScope}:{trimmed}"
            : $"{_settings.KeyPrefix}:{CacheScope}:{trimmed}";
    }
}
