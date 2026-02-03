using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using ClassroomService.Domain.Common;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Infrastructure.Messaging;

/// <summary>
/// Background service that consumes cache invalidation events from UserService
/// </summary>
public class CacheInvalidationConsumer : BackgroundService
{
    private IConsumer<string, string>? _consumer;
    private readonly KafkaSettings _kafkaSettings;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CacheInvalidationConsumer> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _bootstrapServers;

    public CacheInvalidationConsumer(
        IOptions<KafkaSettings> kafkaSettings,
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<CacheInvalidationConsumer> logger)
    {
        _kafkaSettings = kafkaSettings.Value;
        _serviceProvider = serviceProvider;
        _logger = logger;

        // Use appsettings.json configuration (localhost:9092 for host-based services)
        // Aspire's GetConnectionString("kafka") resolves to host.docker.internal which doesn't work for host services
        _bootstrapServers = _kafkaSettings.BootstrapServers;
        _logger.LogInformation("[Kafka Config] CacheInvalidationConsumer using appsettings.json: {BootstrapServers}", _bootstrapServers);
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        _logger.LogInformation("CacheInvalidationConsumer created");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Add delay to let Kafka container fully initialize
        _logger.LogInformation("CacheInvalidationConsumer waiting 5 seconds for Kafka to be ready...");
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        try
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _bootstrapServers,
                GroupId = $"{_kafkaSettings.ConsumerGroup}-cache-invalidation",
                AutoOffsetReset = AutoOffsetReset.Latest, // Only process new invalidations
                EnableAutoCommit = false,
                ClientId = "classroom-service-cache-invalidation-consumer"
            };

            _logger.LogInformation("Creating cache invalidation consumer...");
            _consumer = new ConsumerBuilder<string, string>(config).Build();

            // Subscribe to invalidation topic
            _consumer.Subscribe(_kafkaSettings.UserCacheInvalidationTopic);
            _logger.LogInformation("✓ Subscribed to cache invalidation topic: {Topic}", 
                _kafkaSettings.UserCacheInvalidationTopic);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(stoppingToken);

                    if (consumeResult != null)
                    {
                        await HandleInvalidationEvent(consumeResult, stoppingToken);
                        _consumer.Commit(consumeResult);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming cache invalidation message: {Reason}", ex.Error.Reason);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Cache invalidation consumer operation cancelled");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in cache invalidation consumer");
            throw;
        }
        finally
        {
            _consumer?.Close();
            _logger.LogInformation("Cache invalidation consumer closed");
        }
    }

    private async Task HandleInvalidationEvent(ConsumeResult<string, string> consumeResult, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var invalidationEvent = JsonSerializer.Deserialize<UserCacheInvalidationEvent>(
                consumeResult.Message.Value, _jsonOptions);

            if (invalidationEvent == null)
            {
                _logger.LogWarning("❌ [INVALIDATION ERROR] Failed to deserialize event");
                return;
            }

            _logger.LogInformation(
                "🔔 [CACHE INVALIDATION] UserId: {UserId} | Type: {Type} | Reason: {Reason}",
                invalidationEvent.UserId, invalidationEvent.Type, invalidationEvent.Reason);

            // Get cache service and invalidate the user
            using var scope = _serviceProvider.CreateScope();
            var cacheService = scope.ServiceProvider.GetRequiredService<IUserInfoCacheService>();
            
            await cacheService.InvalidateUserAsync(invalidationEvent.UserId, cancellationToken);
            stopwatch.Stop();

            _logger.LogInformation("🗑️ [CACHE INVALIDATED] UserId: {UserId} | Processed in {Time}ms", 
                invalidationEvent.UserId, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "❌ [INVALIDATION FAILED] Error handling cache invalidation | Time: {Time}ms", 
                stopwatch.ElapsedMilliseconds);
        }
    }

    public override void Dispose()
    {
        _consumer?.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Cache invalidation event DTO (mirrors UserService event)
/// </summary>
public class UserCacheInvalidationEvent
{
    public Guid UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? Reason { get; set; }
}
