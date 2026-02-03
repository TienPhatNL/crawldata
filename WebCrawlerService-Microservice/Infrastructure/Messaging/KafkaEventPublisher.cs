using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebCrawlerService.Domain.Interfaces;
using WebCrawlerService.Domain.Common;
using System.Text.Json;

namespace WebCrawlerService.Infrastructure.Messaging;

public class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly KafkaSettings _kafkaSettings;
    private readonly ILogger<KafkaEventPublisher> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public KafkaEventPublisher(IOptions<KafkaSettings> kafkaSettings, ILogger<KafkaEventPublisher> logger)
    {
        _kafkaSettings = kafkaSettings.Value;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var config = new ProducerConfig
        {
            BootstrapServers = _kafkaSettings.BootstrapServers,
            Acks = Acks.All,
            MessageTimeoutMs = 10000,
            EnableIdempotence = true,
            MaxInFlight = 1,
            CompressionType = CompressionType.Snappy,
            BrokerAddressFamily = BrokerAddressFamily.V4
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default) where T : BaseEvent
    {
        var eventType = typeof(T).Name;
        var eventContent = JsonSerializer.Serialize(domainEvent, _jsonOptions);
        
        await PublishAsync(eventType, eventContent, cancellationToken);
    }

    public async Task PublishAsync(string eventType, string eventContent, CancellationToken cancellationToken = default)
    {
        var topicName = GetTopicName(eventType);
        
        try
        {
            var message = new Message<string, string>
            {
                Key = Guid.NewGuid().ToString(),
                Value = eventContent,
                Headers = new Headers
                {
                    { "event-type", System.Text.Encoding.UTF8.GetBytes(eventType) },
                    { "timestamp", System.Text.Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")) },
                    { "service", System.Text.Encoding.UTF8.GetBytes("WebCrawlerService") }
                }
            };

            var deliveryResult = await _producer.ProduceAsync(topicName, message, cancellationToken);
            
            _logger.LogDebug("Message published to topic {TopicName}, partition {Partition}, offset {Offset}",
                deliveryResult.TopicPartition.Topic,
                deliveryResult.TopicPartition.Partition.Value,
                deliveryResult.Offset.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to publish message to topic {TopicName}: {ErrorReason}", topicName, ex.Error.Reason);
            throw;
        }
    }

    private string GetTopicName(string eventType)
    {
        // Publish all crawler events to unified "crawler-events" topic for NotificationService
        return "crawler-events";
    }

    public void Dispose()
    {
        _producer?.Flush(TimeSpan.FromSeconds(10));
        _producer?.Dispose();
        GC.SuppressFinalize(this);
    }
}

public class KafkaSettings
{
    public const string SectionName = "KafkaSettings";

    public string BootstrapServers { get; set; } = "localhost:9092";

    // Job lifecycle topics
    public string JobStartedTopic { get; init; } = "crawler.job.started";
    public string JobCompletedTopic { get; init; } = "crawler.job.completed";
    public string JobProgressTopic { get; init; } = "crawler.job.progress";
    public string JobStatusChangedTopic { get; init; } = "crawler.job.status-changed";
    public string CrawlerFailedTopic { get; init; } = "crawler.job.failed";

    // URL-level crawl topics
    public string UrlCrawlStartedTopic { get; init; } = "crawler.url.started";
    public string UrlCrawlCompletedTopic { get; init; } = "crawler.url.completed";
    public string UrlCrawlFailedTopic { get; init; } = "crawler.url.failed";

    // Quota usage topic (consumed by UserService)
    public string QuotaUsageTopic { get; init; } = "crawler.quota.usage";

    // Default fallback
    public string DefaultTopic { get; init; } = "crawler.events";

    // Consumer group for ClassroomService
    public string ConsumerGroupId { get; init; } = "classroom-service-group";
}
