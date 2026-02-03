using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Domain.Common;
using System.Text.Json;

namespace NotificationService.Infrastructure.Messaging;

public abstract class BaseKafkaConsumer : BackgroundService
{
    private IConsumer<string, string>? _consumer;
    protected readonly IServiceProvider _serviceProvider;
    protected readonly ILogger _logger;
    private readonly KafkaSettings _kafkaSettings;
    private readonly string _consumerGroupSuffix;
    protected readonly JsonSerializerOptions _jsonOptions;

    protected BaseKafkaConsumer(
        IOptions<KafkaSettings> kafkaSettings,
        IServiceProvider serviceProvider,
        ILogger logger,
        string consumerGroupSuffix)
    {
        _kafkaSettings = kafkaSettings.Value;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _consumerGroupSuffix = consumerGroupSuffix;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        _logger.LogInformation("{ConsumerType} created, will connect to: {BootstrapServers}",
            GetType().Name, _kafkaSettings.BootstrapServers);
    }

    protected abstract string[] GetTopics();

    protected abstract Task HandleMessageAsync(ConsumeResult<string, string> consumeResult, CancellationToken cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Add delay to let Kafka container fully initialize
        _logger.LogInformation("{ConsumerType} waiting 5 seconds for Kafka to be ready...", GetType().Name);
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        try
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _kafkaSettings.BootstrapServers,
                GroupId = $"{_kafkaSettings.ConsumerGroup}-{_consumerGroupSuffix}",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
                ClientId = $"notification-service-{_consumerGroupSuffix}",
                SessionTimeoutMs = 10000,
                SocketTimeoutMs = 10000
            };

            _logger.LogInformation("Creating Kafka consumer...");
            _consumer = new ConsumerBuilder<string, string>(config).Build();
            _logger.LogInformation("✓ Kafka consumer created successfully with bootstrap servers: {BootstrapServers}, group ID: {GroupId}",
                _kafkaSettings.BootstrapServers, config.GroupId);

            var topics = GetTopics();
            _logger.LogInformation("Subscribing to topics...");
            _consumer.Subscribe(topics);
            _logger.LogInformation("✓ Subscribed to topics: {Topics}", string.Join(", ", topics));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(stoppingToken);

                    if (consumeResult != null)
                    {
                        await HandleMessageAsync(consumeResult, stoppingToken);
                        _consumer.Commit(consumeResult);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming message: {Reason}", ex.Error.Reason);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("{ConsumerType} is stopping...", GetType().Name);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in {ConsumerType}", GetType().Name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in {ConsumerType}", GetType().Name);
        }
    }

    public override void Dispose()
    {
        _consumer?.Close();
        _consumer?.Dispose();
        base.Dispose();
    }
}
