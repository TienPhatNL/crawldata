using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using UserService.Domain.Common;
using UserService.Domain.Events;

namespace UserService.Infrastructure.Messaging;

/// <summary>
/// Publisher for cache invalidation events via Kafka
/// </summary>
public class CacheInvalidationPublisher
{
    private readonly IProducer<string, string> _producer;
    private readonly KafkaSettings _kafkaSettings;
    private readonly ILogger<CacheInvalidationPublisher> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _bootstrapServers;

    public CacheInvalidationPublisher(
        IOptions<KafkaSettings> kafkaSettings,
        IConfiguration configuration,
        ILogger<CacheInvalidationPublisher> logger)
    {
        _kafkaSettings = kafkaSettings.Value;
        _logger = logger;

        // Use appsettings.json configuration (localhost:9092 for host-based services)
        // Aspire's GetConnectionString("kafka") resolves to host.docker.internal which doesn't work for host services
        _bootstrapServers = _kafkaSettings.BootstrapServers;
        _logger.LogInformation("[Kafka Config] CacheInvalidationPublisher using appsettings.json: {BootstrapServers}", _bootstrapServers);
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _bootstrapServers,
            Acks = Acks.All, // Required for idempotence
            EnableIdempotence = true, // Ensures exactly-once delivery
            MaxInFlight = 5, // Max unacknowledged requests
            ClientId = "user-service-cache-invalidation-producer"
        };

        _producer = new ProducerBuilder<string, string>(producerConfig).Build();
        _logger.LogInformation("CacheInvalidationPublisher initialized with bootstrap servers: {BootstrapServers}", 
            _bootstrapServers);
    }

    /// <summary>
    /// Publish a cache invalidation event for a user
    /// </summary>
    public async Task PublishUserInvalidationAsync(
        Guid userId, 
        InvalidationType type, 
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var invalidationEvent = new UserCacheInvalidationEvent
            {
                UserId = userId,
                Type = type,
                Timestamp = DateTime.UtcNow,
                Reason = reason
            };

            var messageValue = JsonSerializer.Serialize(invalidationEvent, _jsonOptions);
            var message = new Message<string, string>
            {
                Key = userId.ToString(),
                Value = messageValue,
                Headers = new Headers
                {
                    { "event-type", System.Text.Encoding.UTF8.GetBytes(type.ToString()) },
                    { "timestamp", System.Text.Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")) },
                    { "service", System.Text.Encoding.UTF8.GetBytes("UserService") }
                }
            };

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await _producer.ProduceAsync(
                _kafkaSettings.UserCacheInvalidationTopic,
                message,
                cancellationToken);
            stopwatch.Stop();

            _logger.LogInformation(
                "📢 [INVALIDATION PUBLISHED] UserId: {UserId} | Type: {Type} | Reason: {Reason} | Partition: {Partition} | Offset: {Offset} | Time: {Time}ms",
                userId, type, reason ?? "N/A", result.Partition.Value, result.Offset.Value, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [INVALIDATION PUBLISH FAILED] UserId: {UserId} | Type: {Type}", userId, type);
            // Don't throw - cache invalidation failures shouldn't break the main flow
        }
    }

    /// <summary>
    /// Publish multiple cache invalidation events
    /// </summary>
    public async Task PublishBatchInvalidationAsync(
        IEnumerable<Guid> userIds,
        InvalidationType type,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var tasks = userIds.Select(userId => 
            PublishUserInvalidationAsync(userId, type, reason, cancellationToken));
        
        await Task.WhenAll(tasks);
    }

    public void Dispose()
    {
        _producer?.Flush(TimeSpan.FromSeconds(10));
        _producer?.Dispose();
    }
}
