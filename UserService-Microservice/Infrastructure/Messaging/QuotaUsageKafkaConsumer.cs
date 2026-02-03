using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UserService.Domain.Common;
using UserService.Domain.Services;
using UserService.Infrastructure.Messaging.Contracts;
using UserService.Infrastructure.Repositories;

namespace UserService.Infrastructure.Messaging;

/// <summary>
/// Consumes crawl usage events from Kafka and updates crawl quota + snapshot synchronously.
/// </summary>
public class QuotaUsageKafkaConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<QuotaUsageKafkaConsumer> _logger;
    private readonly KafkaSettings _kafkaSettings;
    private readonly JsonSerializerOptions _serializerOptions;
    private IConsumer<string, string>? _consumer;

    private const int TopicPartitions = 3;
    private const short TopicReplicationFactor = 1;
    private const int TopicCreationAttempts = 5;
    private static readonly TimeSpan TopicRetryDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(5);

    public QuotaUsageKafkaConsumer(
        IServiceProvider serviceProvider,
        IOptions<KafkaSettings> kafkaOptions,
        ILogger<QuotaUsageKafkaConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _kafkaSettings = kafkaOptions.Value;
        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(StartupDelay, stoppingToken);

        if (!await EnsureTopicExistsAsync(stoppingToken))
        {
            _logger.LogWarning(
                "Quota usage consumer not started because topic {Topic} is unavailable",
                _kafkaSettings.QuotaUsageTopic);
            return;
        }

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _kafkaSettings.BootstrapServers,
            GroupId = _kafkaSettings.QuotaUsageConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            ClientId = _kafkaSettings.QuotaUsageConsumerGroup,
            SessionTimeoutMs = 10000
        };

        _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        _consumer.Subscribe(_kafkaSettings.QuotaUsageTopic);
        _logger.LogInformation(
            "Quota usage consumer subscribed to {Topic} on {Bootstrap}",
            _kafkaSettings.QuotaUsageTopic,
            _kafkaSettings.BootstrapServers);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result = null;

                try
                {
                    result = _consumer.Consume(stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogWarning(ex, "Kafka consume error in quota usage consumer; retrying");
                    continue;
                }
                catch (KafkaException ex)
                {
                    _logger.LogWarning(ex, "Kafka transport error in quota usage consumer; delaying");
                    await Task.Delay(TopicRetryDelay, stoppingToken);
                    continue;
                }

                if (result == null)
                {
                    continue;
                }

                try
                {
                    await HandleMessageAsync(result, stoppingToken);
                    _consumer.Commit(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed processing quota usage message. Offsetting to next.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // expected on shutdown
        }
        finally
        {
            _consumer?.Close();
        }
    }

    private async Task<bool> EnsureTopicExistsAsync(CancellationToken stoppingToken)
    {
        var adminConfig = new AdminClientConfig
        {
            BootstrapServers = _kafkaSettings.BootstrapServers
        };

        using var adminClient = new AdminClientBuilder(adminConfig).Build();

        for (var attempt = 1; attempt <= TopicCreationAttempts && !stoppingToken.IsCancellationRequested; attempt++)
        {
            try
            {
                var specs = new[]
                {
                    new TopicSpecification
                    {
                        Name = _kafkaSettings.QuotaUsageTopic,
                        NumPartitions = TopicPartitions,
                        ReplicationFactor = TopicReplicationFactor
                    }
                };

                await adminClient.CreateTopicsAsync(specs);
                _logger.LogInformation(
                    "Created Kafka topic {Topic} for quota usage consumer (attempt {Attempt})",
                    _kafkaSettings.QuotaUsageTopic,
                    attempt);
                return true;
            }
            catch (CreateTopicsException ex) when (ex.Results.Count > 0 && ex.Results[0].Error.Code == ErrorCode.TopicAlreadyExists)
            {
                _logger.LogInformation("Kafka topic {Topic} already exists", _kafkaSettings.QuotaUsageTopic);
                return true;
            }
            catch (KafkaException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to ensure Kafka topic {Topic} (attempt {Attempt}/{MaxAttempts})",
                    _kafkaSettings.QuotaUsageTopic,
                    attempt,
                    TopicCreationAttempts);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Unexpected error ensuring Kafka topic {Topic} (attempt {Attempt}/{MaxAttempts})",
                    _kafkaSettings.QuotaUsageTopic,
                    attempt,
                    TopicCreationAttempts);
            }

            try
            {
                await Task.Delay(TopicRetryDelay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        return false;
    }

    private async Task HandleMessageAsync(ConsumeResult<string, string> message, CancellationToken cancellationToken)
    {
        CrawlQuotaUsageEvent? usageEvent = null;
        try
        {
            usageEvent = JsonSerializer.Deserialize<CrawlQuotaUsageEvent>(message.Message.Value, _serializerOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invalid quota usage payload: {Payload}", message.Message.Value);
            return;
        }

        if (usageEvent == null)
        {
            _logger.LogWarning("Received empty quota usage payload");
            return;
        }

        if (usageEvent.UnitsConsumed == 0)
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var snapshotService = scope.ServiceProvider.GetRequiredService<IQuotaSnapshotService>();

        var user = await unitOfWork.Users.GetByIdAsync(usageEvent.UserId, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("Quota usage event for unknown user {UserId}", usageEvent.UserId);
            return;
        }

        var newUsage = user.CrawlQuotaUsed + usageEvent.UnitsConsumed;
        if (newUsage < 0)
        {
            newUsage = 0;
        }

        user.CrawlQuotaUsed = Math.Min(user.CrawlQuotaLimit, newUsage);
        user.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.Users.UpdateAsync(user, cancellationToken);
        await snapshotService.UpsertFromUserAsync(
            user,
            usageEvent.Source ?? "crawler-usage",
            isOverride: false,
            synchronizedAt: usageEvent.OccurredAt,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Processed quota usage for user {UserId}. Δ={Delta}, Used={Used}/{Limit}",
            user.Id,
            usageEvent.UnitsConsumed,
            user.CrawlQuotaUsed,
            user.CrawlQuotaLimit);
    }
}
