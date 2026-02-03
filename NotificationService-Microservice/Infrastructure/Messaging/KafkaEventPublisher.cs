using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Domain.Common;
using System.Text.Json;

namespace NotificationService.Infrastructure.Messaging;

public class KafkaEventPublisher
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventPublisher> _logger;
    private readonly MessageCorrelationManager _correlationManager;

    public KafkaEventPublisher(
        IOptions<KafkaSettings> kafkaSettings,
        ILogger<KafkaEventPublisher> logger,
        MessageCorrelationManager correlationManager)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = kafkaSettings.Value.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
            MaxInFlight = 5,
            MessageSendMaxRetries = 3,
            RetryBackoffMs = 1000
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
        _logger = logger;
        _correlationManager = correlationManager;
    }

    public async Task PublishAsync<T>(string topic, T @event, string? key = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var correlationId = _correlationManager.GetOrCreateCorrelationId();
            var eventData = new
            {
                EventId = Guid.NewGuid(),
                OccurredOn = DateTime.UtcNow,
                CorrelationId = correlationId,
                Data = @event
            };

            var message = new Message<string, string>
            {
                Key = key ?? Guid.NewGuid().ToString(),
                Value = JsonSerializer.Serialize(eventData),
                Headers = new Headers
                {
                    { "correlation-id", System.Text.Encoding.UTF8.GetBytes(correlationId) },
                    { "event-type", System.Text.Encoding.UTF8.GetBytes(typeof(T).Name) }
                }
            };

            var result = await _producer.ProduceAsync(topic, message, cancellationToken);
            _logger.LogInformation("Published event {EventType} to topic {Topic} with key {Key}", 
                typeof(T).Name, topic, key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType} to topic {Topic}", 
                typeof(T).Name, topic);
            throw;
        }
    }
}
