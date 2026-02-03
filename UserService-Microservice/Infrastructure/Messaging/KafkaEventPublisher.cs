using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using UserService.Domain.Common;

namespace UserService.Infrastructure.Messaging;

/// <summary>
/// Kafka event publisher for UserService
/// </summary>
public class KafkaEventPublisher : IDisposable
{
    private IProducer<string, string>? _producer;
    private readonly object _producerLock = new object();
    private readonly KafkaSettings _kafkaSettings;
    private readonly ILogger<KafkaEventPublisher> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public KafkaEventPublisher(IOptions<KafkaSettings> kafkaSettings, ILogger<KafkaEventPublisher> logger)
    {
        _kafkaSettings = kafkaSettings.Value;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
        
        _logger.LogInformation("[Kafka Config] KafkaEventPublisher created, will connect to: {BootstrapServers}", 
            _kafkaSettings.BootstrapServers);
    }

    private IProducer<string, string> GetProducer()
    {
        if (_producer != null)
            return _producer;

        lock (_producerLock)
        {
            if (_producer != null)
                return _producer;

            var config = new ProducerConfig
            {
                BootstrapServers = _kafkaSettings.BootstrapServers,
                Acks = Acks.All,
                MessageTimeoutMs = 10000,
                EnableIdempotence = true,
                MaxInFlight = 1,
                CompressionType = CompressionType.Snappy,
                ClientId = "userservice-producer"
            };

            _producer = new ProducerBuilder<string, string>(config).Build();
            _logger.LogInformation("[Kafka Config] Kafka producer initialized with bootstrap servers: {BootstrapServers}", 
                _kafkaSettings.BootstrapServers);
            
            return _producer;
        }
    }

    /// <summary>
    /// Publish a domain event to Kafka
    /// </summary>
    public async Task PublishEventAsync<TEvent>(string topic, TEvent domainEvent, CancellationToken cancellationToken = default) where TEvent : class
    {
        try
        {
            var eventJson = JsonSerializer.Serialize(domainEvent, _jsonOptions);
            var key = Guid.NewGuid().ToString();

            var message = new Message<string, string>
            {
                Key = key,
                Value = eventJson,
                Headers = new Headers
                {
                    { "timestamp", System.Text.Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")) },
                    { "service", System.Text.Encoding.UTF8.GetBytes("UserService") },
                    { "event-type", System.Text.Encoding.UTF8.GetBytes(typeof(TEvent).Name) }
                }
            };

            var producer = GetProducer();
            var result = await producer.ProduceAsync(topic, message, cancellationToken);
            
            _logger.LogInformation(
                "[Kafka] Published {EventType} to topic '{Topic}' at offset {Offset}", 
                typeof(TEvent).Name, topic, result.Offset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "[Kafka] Failed to publish {EventType} to topic '{Topic}'", 
                typeof(TEvent).Name, topic);
            throw;
        }
    }

    public void Dispose()
    {
        _producer?.Dispose();
        _logger.LogInformation("[Kafka] KafkaEventPublisher disposed");
    }
}
