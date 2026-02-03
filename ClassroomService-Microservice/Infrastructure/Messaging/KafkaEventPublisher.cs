using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ClassroomService.Domain.Common;
using System.Text.Json;

namespace ClassroomService.Infrastructure.Messaging;

/// <summary>
/// Kafka event publisher for ClassroomService
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
        
        // Producer will be initialized lazily on first use to prevent blocking startup
        _logger.LogInformation("KafkaEventPublisher created, will connect to: {BootstrapServers}", 
            _kafkaSettings.BootstrapServers);
    }

    private IProducer<string, string> GetProducer()
    {
        // Double-check locking pattern for thread-safe lazy initialization
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
                ClientId = "classroom-service-producer"
            };

            _producer = new ProducerBuilder<string, string>(config).Build();
            _logger.LogInformation("Kafka producer initialized (lazy) with bootstrap servers: {BootstrapServers}", 
                _kafkaSettings.BootstrapServers);
            
            return _producer;
        }
    }

    /// <summary>
    /// Publish a message to a specific topic
    /// </summary>
    public async Task PublishAsync(string topic, string key, string messageContent, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new Message<string, string>
            {
                Key = key,
                Value = messageContent,
                Headers = new Headers
                {
                    { "timestamp", System.Text.Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")) },
                    { "service", System.Text.Encoding.UTF8.GetBytes("ClassroomService") }
                }
            };

            var producer = GetProducer();
            var deliveryResult = await producer.ProduceAsync(topic, message, cancellationToken);
            
            _logger.LogDebug("Message published to topic {TopicName}, partition {Partition}, offset {Offset}, key: {Key}",
                deliveryResult.TopicPartition.Topic,
                deliveryResult.TopicPartition.Partition.Value,
                deliveryResult.Offset.Value,
                key);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to publish message to topic {TopicName}: {ErrorReason}", topic, ex.Error.Reason);
            throw;
        }
    }

    /// <summary>
    /// Publish an object message to a specific topic with event type header
    /// </summary>
    public async Task PublishAsync<T>(string topic, string key, T message, CancellationToken cancellationToken = default)
    {
        try
        {
            var messageContent = JsonSerializer.Serialize(message, _jsonOptions);
            var eventType = typeof(T).Name;

            var kafkaMessage = new Message<string, string>
            {
                Key = key,
                Value = messageContent,
                Headers = new Headers
                {
                    { "timestamp", System.Text.Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")) },
                    { "service", System.Text.Encoding.UTF8.GetBytes("ClassroomService") },
                    { "event-type", System.Text.Encoding.UTF8.GetBytes(eventType) }
                }
            };

            var producer = GetProducer();
            var deliveryResult = await producer.ProduceAsync(topic, kafkaMessage, cancellationToken);
            
            _logger.LogDebug("Event {EventType} published to topic {TopicName}, partition {Partition}, offset {Offset}, key: {Key}",
                eventType,
                deliveryResult.TopicPartition.Topic,
                deliveryResult.TopicPartition.Partition.Value,
                deliveryResult.Offset.Value,
                key);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType} to topic {TopicName}: {ErrorReason}", 
                typeof(T).Name, topic, ex.Error.Reason);
            throw;
        }
    }

    public void Dispose()
    {
        _producer?.Flush(TimeSpan.FromSeconds(10));
        _producer?.Dispose();
        GC.SuppressFinalize(this);
    }
}
