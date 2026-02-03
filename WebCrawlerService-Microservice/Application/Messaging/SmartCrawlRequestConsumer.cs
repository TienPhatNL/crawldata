using Confluent.Kafka;
using System.Text.Json;
using WebCrawlerService.Application.Services.Crawl4AI;

namespace WebCrawlerService.Application.Messaging;

/// <summary>
/// Consumes smart crawl requests from ClassroomService
/// </summary>
public class SmartCrawlRequestConsumer : BackgroundService
{
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly ILogger<SmartCrawlRequestConsumer> _logger;
	private readonly IConfiguration _configuration;
	private IConsumer<string, string>? _consumer;
	private DateTime _lastTopicWarning = DateTime.MinValue;

	public SmartCrawlRequestConsumer(
		IServiceScopeFactory scopeFactory,
		ILogger<SmartCrawlRequestConsumer> logger,
		IConfiguration configuration)
	{
		_scopeFactory = scopeFactory;
		_logger = logger;
		_configuration = configuration;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("SmartCrawlRequestConsumer starting...");

		await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

		var bootstrapServers = _configuration["KafkaSettings:BootstrapServers"] ?? "localhost:9092";
		var topic = _configuration["KafkaSettings:SmartCrawlRequestTopic"] ?? "classroom.crawler.request";
		var groupId = _configuration["KafkaSettings:CrawlerConsumerGroup"] ?? "webcrawler-consumer";

		var config = new ConsumerConfig
		{
			BootstrapServers = bootstrapServers,
			GroupId = groupId,
			AutoOffsetReset = AutoOffsetReset.Earliest,
			EnableAutoCommit = false,
			AllowAutoCreateTopics = true,
			ReconnectBackoffMs = 1000,
			BrokerAddressFamily = BrokerAddressFamily.V4
		};

		// === SAFE CONSUMER INIT ===
		try
		{
			_consumer = new ConsumerBuilder<string, string>(config)
				.SetErrorHandler((_, e) =>
				{
					if (e.Code != ErrorCode.UnknownTopicOrPart)
						_logger.LogError("Kafka error: {Reason}", e.Reason);
				})
				.Build();

			_consumer.Subscribe(topic);
			_logger.LogInformation("Subscribed to topic: {Topic}", topic);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to subscribe to topic {Topic}. Will retry in loop.", topic);
		}

		int consecutiveErrors = 0;

		// === POLLING LOOP ===
		while (!stoppingToken.IsCancellationRequested)
		{
			if (_consumer == null)
			{
				await Task.Delay(5000, stoppingToken);
				continue;
			}

			try
			{
				var result = _consumer.Consume(TimeSpan.FromSeconds(1));
				if (result?.Message == null) continue;

				// DEBUG: Log raw JSON to see MaxPages value before deserialization
				_logger.LogInformation("🔍 DEBUG: Raw Kafka message: {Message}", result.Message.Value);

				var request = JsonSerializer.Deserialize<SmartCrawlRequestEvent>(
					result.Message.Value,
					new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

				if (request == null)
				{
					_logger.LogWarning("Failed to deserialize message at offset {Offset}", result.Offset);
					_consumer.Commit(result);
					continue;
				}

				_logger.LogInformation("Received crawl request: {JobId}, MaxPages: {MaxPages}", request.JobId, request.MaxPages?.ToString() ?? "null");
				_ = ProcessCrawlRequestAsync(request, stoppingToken);
				_consumer.Commit(result);
				consecutiveErrors = 0;
			}
			catch (ConsumeException ex) when (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
			{
				var now = DateTime.UtcNow;
				if ((now - _lastTopicWarning).TotalSeconds > 30)
				{
					_logger.LogWarning("Topic {Topic} not found. Waiting for first publish...", topic);
					_lastTopicWarning = now;
				}

				consecutiveErrors++;
				var delay = consecutiveErrors switch
				{
					<= 3 => 5,
					<= 6 => 15,
					<= 9 => 30,
					_ => 60
				};
				await Task.Delay(TimeSpan.FromSeconds(delay), stoppingToken);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in consumer loop");
				consecutiveErrors++;
				await Task.Delay(TimeSpan.FromSeconds(Math.Min(consecutiveErrors * 5, 60)), stoppingToken);
			}
		}

		// === CLEANUP ===
		try
		{
			_consumer?.Close();
			_consumer?.Dispose();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error closing consumer");
		}

		_logger.LogInformation("SmartCrawlRequestConsumer stopped.");
	}

	private async Task ProcessCrawlRequestAsync(SmartCrawlRequestEvent request, CancellationToken ct)
	{
		try
		{
			using var scope = _scopeFactory.CreateScope();
			var service = scope.ServiceProvider.GetRequiredService<ISmartCrawlerOrchestrationService>();
			await service.ExecuteIntelligentCrawlFromEventAsync(request, ct);
			_logger.LogInformation("Processed crawl request for JobId: {JobId}", request.JobId);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to process crawl request {JobId}", request.JobId);
		}
	}

	public override void Dispose()
	{
		try { _consumer?.Dispose(); } catch { }
		base.Dispose();
	}
}

public class SmartCrawlRequestEvent
{
	public Guid JobId { get; set; }
	public Guid ConversationId { get; set; }
	public Guid AssignmentId { get; set; }
	public Guid? GroupId { get; set; }
	public Guid SenderId { get; set; }
	public string SenderName { get; set; } = string.Empty;
	public string Url { get; set; } = string.Empty;
	public string UserPrompt { get; set; } = string.Empty;
	public int? MaxPages { get; set; } // Nullable: null = empty UI field (Python handles default)
	public bool EnableNavigationPlanning { get; set; } = true;
	public DateTime Timestamp { get; set; }
	public string? MetadataJson { get; set; }
}
