using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ClassroomService.Domain.Common;
using ClassroomService.Domain.Messages;
using System.Text.Json;

namespace ClassroomService.Infrastructure.Messaging;

/// <summary>
/// Background service that consumes Kafka messages (responses from UserService)
/// </summary>
public class ClassroomKafkaConsumer : BackgroundService
{
    private IConsumer<string, string>? _consumer;
    private readonly MessageCorrelationManager _correlationManager;
    private readonly KafkaSettings _kafkaSettings;
    private readonly ILogger<ClassroomKafkaConsumer> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ClassroomKafkaConsumer(
        IOptions<KafkaSettings> kafkaSettings,
        MessageCorrelationManager correlationManager,
        ILogger<ClassroomKafkaConsumer> logger)
    {
        _kafkaSettings = kafkaSettings.Value;
        _correlationManager = correlationManager;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        // Consumer will be initialized in ExecuteAsync to prevent blocking startup
        _logger.LogInformation("ClassroomKafkaConsumer created, will connect to: {BootstrapServers}", 
            _kafkaSettings.BootstrapServers);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Add delay to let Kafka container fully initialize
        _logger.LogInformation("ClassroomKafkaConsumer waiting 5 seconds for Kafka to be ready...");
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        try
        {
            // Initialize consumer here to avoid blocking startup
            var config = new ConsumerConfig
            {
                BootstrapServers = _kafkaSettings.BootstrapServers,
                GroupId = _kafkaSettings.ConsumerGroup,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
                ClientId = "classroom-service-consumer",
                SessionTimeoutMs = 10000,
                SocketTimeoutMs = 10000
            };

            _logger.LogInformation("Creating Kafka consumer...");
            _consumer = new ConsumerBuilder<string, string>(config).Build();
            _logger.LogInformation("✓ Kafka consumer created successfully with bootstrap servers: {BootstrapServers}, group ID: {GroupId}", 
                _kafkaSettings.BootstrapServers, _kafkaSettings.ConsumerGroup);

            // Subscribe to response topics
            _logger.LogInformation("Subscribing to topics...");
            _consumer.Subscribe(new[]
            {
                _kafkaSettings.UserQueryResponseTopic,
                _kafkaSettings.UserValidationResponseTopic,
                _kafkaSettings.StudentCreationResponseTopic
            });

            _logger.LogInformation("✓ Subscribed to topics: {Topics}", 
                string.Join(", ", new[]
                {
                    _kafkaSettings.UserQueryResponseTopic,
                    _kafkaSettings.UserValidationResponseTopic,
                    _kafkaSettings.StudentCreationResponseTopic
                }));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(stoppingToken);

                    if (consumeResult != null)
                    {
                        await HandleMessage(consumeResult, stoppingToken);
                        _consumer.Commit(consumeResult);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming message: {Reason}", ex.Error.Reason);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Consumer operation cancelled");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in Kafka consumer");
            throw;
        }
        finally
        {
            _consumer?.Close();
            _logger.LogInformation("Kafka consumer closed");
        }
    }

    private async Task HandleMessage(ConsumeResult<string, string> consumeResult, CancellationToken cancellationToken)
    {
        var correlationId = consumeResult.Message.Key;
        var topic = consumeResult.Topic;

        _logger.LogDebug("Received message from topic {Topic} with correlation ID: {CorrelationId}", 
            topic, correlationId);

        try
        {
            switch (topic)
            {
                case var t when t == _kafkaSettings.UserQueryResponseTopic:
                    var queryResponse = JsonSerializer.Deserialize<UserQueryResponse>(
                        consumeResult.Message.Value, _jsonOptions);
                    if (queryResponse != null)
                    {
                        _correlationManager.CompleteRequest(correlationId, queryResponse);
                    }
                    break;

                case var t when t == _kafkaSettings.UserValidationResponseTopic:
                    var validationResponse = JsonSerializer.Deserialize<UserValidationResponse>(
                        consumeResult.Message.Value, _jsonOptions);
                    if (validationResponse != null)
                    {
                        _correlationManager.CompleteRequest(correlationId, validationResponse);
                    }
                    break;

                case var t when t == _kafkaSettings.StudentCreationResponseTopic:
                    var creationResponse = JsonSerializer.Deserialize<StudentCreationResponse>(
                        consumeResult.Message.Value, _jsonOptions);
                    if (creationResponse != null)
                    {
                        _correlationManager.CompleteRequest(correlationId, creationResponse);
                    }
                    break;

                default:
                    _logger.LogWarning("Received message from unknown topic: {Topic}", topic);
                    break;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize message from topic {Topic}", topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling message from topic {Topic}", topic);
        }

        await Task.CompletedTask;
    }

    public override void Dispose()
    {
        _consumer?.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
