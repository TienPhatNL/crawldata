using System.Text.Json;
using Confluent.Kafka;
using MediatR;
using Microsoft.EntityFrameworkCore;
using WebCrawlerService.Domain.Entities;
using WebCrawlerService.Domain.Enums;
using WebCrawlerService.Domain.Events;
using WebCrawlerService.Infrastructure.Contexts;

namespace WebCrawlerService.Application.Messaging;

/// <summary>
/// Kafka consumer for processing crawl job completion events from Python agents
/// </summary>
public class CrawlJobResultConsumer : BackgroundService
{
	private readonly ILogger<CrawlJobResultConsumer> _logger;
	private readonly IServiceProvider _serviceProvider;
	private readonly IConfiguration _configuration;
	private IConsumer<string, string>? _consumer;
	private readonly JsonSerializerOptions _jsonOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	public CrawlJobResultConsumer(
		ILogger<CrawlJobResultConsumer> logger,
		IServiceProvider serviceProvider,
		IConfiguration configuration)
	{
		_logger = logger;
		_serviceProvider = serviceProvider;
		_configuration = configuration;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("CrawlJobResultConsumer starting...");

		// Wait for Kafka to initialize
		await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

		var bootstrapServers = _configuration["KafkaSettings:BootstrapServers"] ?? "localhost:9092";
		_logger.LogInformation("Connecting to Kafka at: {BootstrapServers}", bootstrapServers);

		var config = new ConsumerConfig
		{
			BootstrapServers = bootstrapServers,
			GroupId = "webcrawler-result-consumer",
			AutoOffsetReset = AutoOffsetReset.Earliest,
			EnableAutoCommit = false,
			EnableAutoOffsetStore = false,
			// Improve connection resilience
			ReconnectBackoffMs = 1000,
			ReconnectBackoffMaxMs = 10000,
			BrokerAddressFamily = BrokerAddressFamily.V4
		};

		// === SAFE INITIALIZATION ===
		try
		{
			_consumer = new ConsumerBuilder<string, string>(config)
				.SetErrorHandler((_, e) =>
				{
					if (e.Code != ErrorCode.UnknownTopicOrPart)
					{
						_logger.LogError("Kafka error: {Reason}", e.Reason);
					}
				})
				.Build();

			_consumer.Subscribe("crawler.job.progress");
			_logger.LogInformation("Subscribed to topic: crawler.job.progress");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to initialize Kafka consumer. Will retry in loop.");
			// Do NOT return � allow loop to retry
		}

		int missingTopicRetryCount = 0;
		const int maxMissingTopicRetries = 20;

		// === MAIN CONSUMER LOOP ===
		while (!stoppingToken.IsCancellationRequested)
		{
			if (_consumer == null)
			{
				await Task.Delay(5000, stoppingToken);
				continue;
			}

			try
			{
				var consumeResult = _consumer.Consume(TimeSpan.FromSeconds(1));
				if (consumeResult?.Message == null)
					continue;

				var eventData = JsonSerializer.Deserialize<CrawlProgressEvent>(
					consumeResult.Message.Value, _jsonOptions);

				if (eventData == null)
				{
					_logger.LogWarning("Failed to deserialize event at offset {Offset}", consumeResult.Offset);
					_consumer.Commit(consumeResult);
					continue;
				}

				_logger.LogInformation("Received event: {EventType} for job {JobId}", eventData.EventType, eventData.JobId);

				// Route events to appropriate handlers
				switch (eventData.EventType)
				{
					case "CrawlJobCompleted":
						_ = ProcessCrawlCompletionAsync(eventData, stoppingToken);
						break;
					
					case "CrawlError":
						_ = ProcessCrawlErrorAsync(eventData, stoppingToken);
						break;
					
					// Navigation events
					case "NavigationPlanningStarted":
					case "NavigationPlanningCompleted":
					case "NavigationExecutionStarted":
					case "NavigationStepCompleted":
						_ = ProcessNavigationEventAsync(eventData, stoppingToken);
						break;
					
					// Pagination events
					case "PaginationPageLoaded":
						_ = ProcessPaginationEventAsync(eventData, stoppingToken);
						break;
					
					// Extraction events
					case "DataExtractionStarted":
					case "DataExtractionCompleted":
						_ = ProcessExtractionEventAsync(eventData, stoppingToken);
						break;
					
					// Generic progress events
					default:
						if (eventData.EventType?.Contains("Progress", StringComparison.OrdinalIgnoreCase) == true)
						{
							_ = ProcessCrawlProgressAsync(eventData, stoppingToken);
						}
						break;
				}

				_consumer.Commit(consumeResult);
				_consumer.StoreOffset(consumeResult);
				missingTopicRetryCount = 0; // Reset on success
			}
			catch (ConsumeException ex) when (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
			{
				missingTopicRetryCount++;
				if (missingTopicRetryCount <= maxMissingTopicRetries)
				{
					_logger.LogWarning(
						"Topic 'crawler.job.progress' not found (attempt {Attempt}/{Max})",
						missingTopicRetryCount, maxMissingTopicRetries);
					await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
				}
				else
				{
					_logger.LogError("Topic not created after {Max} attempts. Giving up.", maxMissingTopicRetries);
					await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
				}
			}
			catch (ConsumeException ex)
			{
				_logger.LogError(ex, "Consume error: {Reason}", ex.Error.Reason);
				await Task.Delay(1000, stoppingToken);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Unexpected error in consumer loop");
				await Task.Delay(5000, stoppingToken);
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
			_logger.LogError(ex, "Error during consumer cleanup");
		}

		_logger.LogInformation("CrawlJobResultConsumer stopped.");
	}

	// === PROCESSING METHODS (unchanged logic, just safer scope) ===
	private async Task ProcessCrawlCompletionAsync(CrawlProgressEvent eventData, CancellationToken ct)
	{
		using var scope = _serviceProvider.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<CrawlerDbContext>();
		var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

		try
		{
			if (!Guid.TryParse(eventData.JobId, out var jobId)) return;

			var job = await context.CrawlJobs.FirstOrDefaultAsync(j => j.Id == jobId, ct);
			if (job == null) return;

			var data = eventData.Data;
			var itemsCount = data.GetProperty("items_count").GetInt32();
			var executionTimeMs = data.GetProperty("execution_time_ms").GetDouble();

			// ====================================================================
			// ⚠️ IMPORTANT: Results are already saved by ProcessSynchronousCompletionAsync
			// when Python agent returns HTTP 200 response. This Kafka event is ONLY for
			// progress tracking and metrics update. DO NOT save CrawlResults here to avoid
			// duplicate data (previously caused 2x storage and duplicate items in UI).
			// 
			// See: DUPLICATE-DATA-ISSUE.md for full analysis
			// ====================================================================

			// Check if results already exist (saved by HTTP handler)
			var existingResultsCount = await context.CrawlResults
				.CountAsync(r => r.CrawlJobId == jobId, ct);

			if (existingResultsCount > 0)
			{
				_logger.LogInformation(
					"✅ Kafka event 'CrawlJobCompleted' for job {JobId}: {Count} items in {Time}ms. " +
					"Results already saved by HTTP handler ({ExistingCount} records). Skipping duplicate save.",
					jobId, itemsCount, executionTimeMs, existingResultsCount);
				return; // Early return - no need to update anything
			}

			// If somehow HTTP handler didn't save (edge case: HTTP timeout but Kafka succeeded)
			// Update job status only - do not save results without full data
			_logger.LogWarning(
				"⚠️ Job {JobId} has no results yet (HTTP handler may have failed). " +
				"Updating status only. Results should come from HTTP response, not Kafka event.",
				jobId);

			// Only update status if not already completed
			if (job.Status != JobStatus.Completed)
			{
				job.Status = JobStatus.Completed;
				job.ResultCount = itemsCount;
				job.CompletedAt = DateTime.UtcNow;
				job.UrlsProcessed = itemsCount;
				job.UrlsSuccessful = itemsCount;
				job.UrlsFailed = 0;

				await context.SaveChangesAsync(ct);
			}
			
			// DON'T publish JobCompletedEvent here - SmartCrawlerOrchestrationService already published it
			// with the correct ConversationName after receiving the HTTP response from Python agent.
			// This Kafka event is ONLY for progress tracking, not final result delivery.
			
			_logger.LogInformation(
				"✅ Job {JobId} status updated from Kafka event: {Count} items in {Time}ms. " +
				"ConversationName: '{Name}' (set by HTTP handler)",
				jobId, itemsCount, executionTimeMs, job.ConversationName ?? "<null>");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to process completion for job {JobId}", eventData.JobId);
		}
	}

	private async Task ProcessCrawlErrorAsync(CrawlProgressEvent eventData, CancellationToken ct)
	{
		using var scope = _serviceProvider.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<CrawlerDbContext>();
		var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

		try
		{
			if (!Guid.TryParse(eventData.JobId, out var jobId)) return;
			var job = await context.CrawlJobs.FirstOrDefaultAsync(j => j.Id == jobId, ct);
			if (job == null) return;

			var errorMsg = eventData.Data.GetProperty("errorMessage").GetString() ?? "Unknown error";
			job.Status = JobStatus.Failed;
			job.ErrorMessage = errorMsg;
			job.CompletedAt = DateTime.UtcNow;

			await context.SaveChangesAsync(ct);
			
			// Publish JobCompletedEvent for failed jobs to notify UI
			await mediator.Publish(new JobCompletedEvent(job), ct);
			
			_logger.LogError("Job {JobId} failed: {Error}", jobId, errorMsg);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to process error for job {JobId}", eventData.JobId);
		}
	}

	private async Task ProcessCrawlProgressAsync(CrawlProgressEvent eventData, CancellationToken ct)
	{
		using var scope = _serviceProvider.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<CrawlerDbContext>();
		var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

		try
		{
			if (!Guid.TryParse(eventData.JobId, out var jobId)) return;
			var job = await context.CrawlJobs.FirstOrDefaultAsync(j => j.Id == jobId, ct);
			if (job == null) return;

			var data = eventData.Data;

			int progressPct = data.TryGetProperty("progress_percentage", out var p) ? p.GetInt32()
				: data.TryGetProperty("progressPercentage", out var pc) ? pc.GetInt32() : 0;

			string? currentUrl = data.TryGetProperty("current_url", out var u) ? u.GetString()
				: data.TryGetProperty("currentUrl", out var uc) ? uc.GetString() : null;

			int completed = data.TryGetProperty("completed_urls", out var c) ? c.GetInt32()
				: data.TryGetProperty("completedUrls", out var cc) ? cc.GetInt32() : 0;

			int total = data.TryGetProperty("total_urls", out var t) ? t.GetInt32()
				: data.TryGetProperty("totalUrls", out var tc) ? tc.GetInt32() : 1;

			await mediator.Publish(new JobProgressUpdatedEvent(
				jobId, job.UserId, total, completed, 0, progressPct, currentUrl, DateTime.UtcNow), ct);

			_logger.LogDebug("Progress: {JobId} ? {Pct}% ({Done}/{Total})", jobId, progressPct, completed, total);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to process progress for job {JobId}", eventData.JobId);
		}
	}

	private async Task ProcessNavigationEventAsync(CrawlProgressEvent eventData, CancellationToken ct)
	{
		using var scope = _serviceProvider.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<CrawlerDbContext>();
		var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

		try
		{
			if (!Guid.TryParse(eventData.JobId, out var jobId)) return;
			var job = await context.CrawlJobs.FirstOrDefaultAsync(j => j.Id == jobId, ct);
			if (job == null) return;

			var data = eventData.Data;

			// Extract navigation-specific data
			int? stepNumber = data.TryGetProperty("step_number", out var sn) ? sn.GetInt32() : null;
			int? totalSteps = data.TryGetProperty("total_steps", out var ts) ? ts.GetInt32() : null;
			string? action = data.TryGetProperty("action", out var a) ? a.GetString() : null;
			string? description = data.TryGetProperty("description", out var d) ? d.GetString() : null;
			string? currentUrl = data.TryGetProperty("current_url", out var cu) ? cu.GetString() : null;
			string? targetElement = data.TryGetProperty("target_element", out var te) ? te.GetString() : null;

			await mediator.Publish(new JobNavigationEvent(
				jobId, 
				job.UserId, 
				eventData.EventType,
				stepNumber,
				totalSteps,
				action,
				description,
				currentUrl,
				targetElement,
				DateTime.UtcNow), ct);

			_logger.LogInformation("Navigation event: {EventType} for job {JobId} (Step {Step}/{Total})", 
				eventData.EventType, jobId, stepNumber, totalSteps);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to process navigation event for job {JobId}", eventData.JobId);
		}
	}

	private async Task ProcessPaginationEventAsync(CrawlProgressEvent eventData, CancellationToken ct)
	{
		using var scope = _serviceProvider.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<CrawlerDbContext>();
		var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

		try
		{
			if (!Guid.TryParse(eventData.JobId, out var jobId)) return;
			var job = await context.CrawlJobs.FirstOrDefaultAsync(j => j.Id == jobId, ct);
			if (job == null) return;

			var data = eventData.Data;

			// Extract pagination-specific data
			int pageNumber = data.TryGetProperty("page_number", out var pn) ? pn.GetInt32() : 0;
			int totalPages = data.TryGetProperty("total_pages_collected", out var tp) ? tp.GetInt32() : 0;
			int? maxPages = data.TryGetProperty("max_pages", out var mp) ? mp.GetInt32() : null;
			long pageSize = data.TryGetProperty("page_size_chars", out var ps) ? ps.GetInt64() : 0;
			string pageUrl = data.TryGetProperty("page_url", out var pu) ? pu.GetString() ?? "" : "";

			await mediator.Publish(new JobPaginationEvent(
				jobId,
				job.UserId,
				pageNumber,
				totalPages,
				pageSize,
				pageUrl,
				maxPages,
				DateTime.UtcNow), ct);

			_logger.LogInformation("Pagination: Job {JobId} loaded page {Page}/{Total}", 
				jobId, pageNumber, maxPages ?? totalPages);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to process pagination event for job {JobId}", eventData.JobId);
		}
	}

	private async Task ProcessExtractionEventAsync(CrawlProgressEvent eventData, CancellationToken ct)
	{
		using var scope = _serviceProvider.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<CrawlerDbContext>();
		var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

		try
		{
			if (!Guid.TryParse(eventData.JobId, out var jobId)) return;
			var job = await context.CrawlJobs.FirstOrDefaultAsync(j => j.Id == jobId, ct);
			if (job == null) return;

			var data = eventData.Data;

			// Extract extraction-specific data
			int? totalItems = data.TryGetProperty("total_items_extracted", out var ti) ? ti.GetInt32() : null;
			int? pagesProcessed = data.TryGetProperty("pages_processed", out var pp) ? pp.GetInt32() : null;
			bool? successful = data.TryGetProperty("extraction_successful", out var es) ? es.GetBoolean() : null;
			string? errorMsg = data.TryGetProperty("error_message", out var em) ? em.GetString() : null;
			double? execTime = data.TryGetProperty("execution_time_ms", out var et) ? et.GetDouble() : null;

			await mediator.Publish(new JobExtractionEvent(
				jobId,
				job.UserId,
				eventData.EventType,
				totalItems,
				pagesProcessed,
				successful,
				errorMsg,
				execTime,
				DateTime.UtcNow), ct);

			_logger.LogInformation("Extraction event: {EventType} for job {JobId} ({Items} items from {Pages} pages)", 
				eventData.EventType, jobId, totalItems, pagesProcessed);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to process extraction event for job {JobId}", eventData.JobId);
		}
	}

	public override void Dispose()
	{
		try { _consumer?.Dispose(); } catch { }
		base.Dispose();
	}
}

public class CrawlProgressEvent
{
	public string EventType { get; set; } = string.Empty;
	public string JobId { get; set; } = string.Empty;
	public string UserId { get; set; } = string.Empty;
	public string Timestamp { get; set; } = string.Empty;
	public JsonElement Data { get; set; }
}