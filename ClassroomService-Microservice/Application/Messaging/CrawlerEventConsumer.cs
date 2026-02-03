using Confluent.Kafka;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using ClassroomService.Application.Hubs;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Entities;

namespace ClassroomService.Application.Messaging;

/// <summary>
/// Background service that consumes crawler events from Kafka
/// and broadcasts them to SignalR clients
/// </summary>
public class CrawlerEventConsumer : BackgroundService
{
    private readonly ILogger<CrawlerEventConsumer> _logger;
    private readonly IHubContext<CrawlerChatHub> _hubContext;
    private readonly KafkaConsumerSettings _settings;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private IConsumer<string, string>? _consumer;

    public CrawlerEventConsumer(
        ILogger<CrawlerEventConsumer> logger,
        IHubContext<CrawlerChatHub> hubContext,
        IOptions<KafkaConsumerSettings> settings,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _hubContext = hubContext;
        _settings = settings.Value;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Crawler Event Consumer starting...");

        // Add startup delay to allow Kafka to be ready (matches other consumers)
        _logger.LogInformation("CrawlerEventConsumer waiting 5 seconds for Kafka to be ready...");
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        var config = new ConsumerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            GroupId = _settings.ConsumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
            EnableAutoOffsetStore = true,
            AutoCommitIntervalMs = 5000,
            AllowAutoCreateTopics = true // Allow Kafka to auto-create topics
        };

        try
        {
            _consumer = new ConsumerBuilder<string, string>(config)
                .SetErrorHandler((_, e) =>
                {
                    // Only log errors that aren't about missing topics
                    if (e.Code != ErrorCode.UnknownTopicOrPart)
                    {
                        _logger.LogError("Kafka consumer error: {Reason}", e.Reason);
                    }
                })
                .Build();

            // Subscribe to unified topic where WebCrawlerService publishes all events
            // WebCrawlerService uses a single "crawler-events" topic for all event types
            var topics = new[]
            {
                "crawler-events"
            };

            _consumer.Subscribe(topics);
            _logger.LogInformation("Subscribed to Kafka topic: {Topics}", string.Join(", ", topics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Kafka consumer. Will retry in consumer loop.");
            // Don't throw - allow service to start, consumer will retry in the loop below
            return;
        }

        int missingTopicRetryCount = 0;
        const int maxMissingTopicRetries = 5;

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(TimeSpan.FromSeconds(1));

                    if (consumeResult?.Message != null)
                    {
                        await ProcessMessageAsync(consumeResult.Message, stoppingToken);
                        missingTopicRetryCount = 0; // Reset on successful consume
                    }
                }
                catch (ConsumeException ex) when (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
                {
                    // Topics don't exist yet - this is expected until WebCrawlerService publishes
                    if (missingTopicRetryCount == 0)
                    {
                        _logger.LogWarning("Crawler topics not yet created. Waiting for WebCrawlerService to publish events...");
                    }

                    missingTopicRetryCount++;
                    if (missingTopicRetryCount >= maxMissingTopicRetries)
                    {
                        _logger.LogInformation("Still waiting for crawler topics after {Count} attempts. Will check again in 1 minute.", missingTopicRetryCount);
                        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                        missingTopicRetryCount = 0;
                    }
                    else
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming message from Kafka: {Reason}", ex.Error.Reason);
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when stopping
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in Kafka consumer loop");
                    await Task.Delay(1000, stoppingToken); // Brief delay before retry
                }
            }
        }
        finally
        {
            _consumer?.Close();
            _logger.LogInformation("Crawler Event Consumer stopped");
        }
    }

    private async Task ProcessMessageAsync(Message<string, string> message, CancellationToken cancellationToken)
    {
        try
        {
            // Extract event type from headers (support both "eventType" and "event-type")
            var eventType = GetHeaderValue(message.Headers, "eventType") 
                         ?? GetHeaderValue(message.Headers, "event-type");
            if (string.IsNullOrEmpty(eventType))
            {
                _logger.LogWarning("Message received without eventType or event-type header");
                return;
            }

            _logger.LogDebug("Processing event: {EventType}", eventType);

            // Parse the event payload
            var eventData = JsonSerializer.Deserialize<JsonElement>(message.Value);

            // Route to appropriate handler based on event type
            await (eventType switch
            {
                "JobStartedEvent" => HandleJobStartedAsync(eventData, cancellationToken),
                "JobCompletedEvent" => HandleJobCompletedAsync(eventData, cancellationToken),
                "JobProgressUpdatedEvent" => HandleJobProgressAsync(eventData, cancellationToken),
                "JobStatusChangedEvent" => HandleJobStatusChangedAsync(eventData, cancellationToken),
                "CrawlerFailedEvent" => HandleCrawlerFailedAsync(eventData, cancellationToken),
                "UrlCrawlStartedEvent" => HandleUrlCrawlStartedAsync(eventData, cancellationToken),
                "UrlCrawlCompletedEvent" => HandleUrlCrawlCompletedAsync(eventData, cancellationToken),
                "UrlCrawlFailedEvent" => HandleUrlCrawlFailedAsync(eventData, cancellationToken),
                // Crawl4AI Agent events (from Python agent via Kafka)
                "CrawlJobAccepted" => HandleNavigationEventAsync(eventData, cancellationToken),
                "NavigationPlanningStarted" => HandleNavigationEventAsync(eventData, cancellationToken),
                "NavigationPlanningCompleted" => HandleNavigationEventAsync(eventData, cancellationToken),
                "NavigationExecutionStarted" => HandleNavigationEventAsync(eventData, cancellationToken),
                "NavigationStepCompleted" => HandleNavigationEventAsync(eventData, cancellationToken),
                "PaginationPageLoaded" => HandleNavigationEventAsync(eventData, cancellationToken),
                "DataExtractionStarted" => HandleNavigationEventAsync(eventData, cancellationToken),
                "DataExtractionCompleted" => HandleNavigationEventAsync(eventData, cancellationToken),
                "EmbeddingGenerationStarted" => HandleNavigationEventAsync(eventData, cancellationToken),
                "EmbeddingGenerationCompleted" => HandleNavigationEventAsync(eventData, cancellationToken),
                "CrawlJobCompleted" => HandleCrawl4AIJobCompletedAsync(eventData, cancellationToken), // OPTIMIZED event with embeddings
                "CrawlJobFailed" => HandleCrawlerFailedAsync(eventData, cancellationToken),
                _ => Task.CompletedTask
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Kafka message");
        }
    }

    // ========== Event Handlers ==========

    private async Task HandleJobStartedAsync(JsonElement eventData, CancellationToken cancellationToken)
    {
        var jobId = eventData.GetProperty("jobId").GetGuid();
        var userId = eventData.GetProperty("userId").GetGuid();

        _logger.LogInformation("Job {JobId} started for user {UserId}", jobId, userId);

        // Broadcast to job subscribers (matching HTML expectation)
        var initiatedData = new 
        { 
            crawlJobId = jobId, 
            userId, 
            status = "running", // Frontend expects string status
            timestamp = DateTime.UtcNow 
        };
        await Task.WhenAll(
            _hubContext.Clients.Group($"crawljob_{jobId}").SendAsync("CrawlInitiated", initiatedData, cancellationToken),
            BroadcastToRelevantGroupsAsync(jobId, "CrawlInitiated", initiatedData, cancellationToken)
        );
    }

    private async Task HandleJobCompletedAsync(JsonElement eventData, CancellationToken cancellationToken)
    {
        var jobId = eventData.GetProperty("jobId").GetGuid();
        var userId = eventData.GetProperty("userId").GetGuid();
        var urlsProcessed = eventData.GetProperty("urlsProcessed").GetInt32();
        var urlsSuccessful = eventData.GetProperty("urlsSuccessful").GetInt32();
        var urlsFailed = eventData.GetProperty("urlsFailed").GetInt32();
        
        // Extract conversation_name from JobCompletedEvent (from WebCrawlerService)
        string? conversationName = eventData.TryGetProperty("conversationName", out var nameProp) ? nameProp.GetString() : null;

        _logger.LogInformation("Job {JobId} completed: ConversationName: '{ConversationName}', {Successful}/{Total} URLs successful",
            jobId, conversationName ?? "<null>", urlsSuccessful, urlsProcessed);

        // Map to frontend-friendly status string
        var statusString = urlsSuccessful > 0 ? "completed" : "failed";

        var completionData = new
        {
            jobId,
            userId,
            conversationName,
            urlsProcessed,
            urlsSuccessful,
            urlsFailed,
            status = statusString, // Frontend expects string status
            timestamp = DateTime.UtcNow
        };

        // Broadcast to multiple groups and update chat message with results
        await Task.WhenAll(
            _hubContext.Clients.Group($"crawljob_{jobId}").SendAsync("CrawlJobCompleted", completionData, cancellationToken),
            BroadcastToRelevantGroupsAsync(jobId, "CrawlJobCompleted", completionData, cancellationToken),
            UpdateChatMessageWithResultsAsync(jobId, conversationName, cancellationToken) // Now includes conversation_name from JobCompletedEvent
        );
    }

    private async Task HandleJobProgressAsync(JsonElement eventData, CancellationToken cancellationToken)
    {
        var jobId = eventData.GetProperty("jobId").GetGuid();
        var totalUrls = eventData.GetProperty("totalUrls").GetInt32();
        var completedUrls = eventData.GetProperty("completedUrls").GetInt32();
        var progressPercentage = eventData.GetProperty("progressPercentage").GetDouble();
        var currentUrl = eventData.TryGetProperty("currentUrl", out var urlProp) ? urlProp.GetString() : null;

        var progressData = new
        {
            jobId,
            progress = (int)progressPercentage, // HTML expects 'progress' field
            progressPercentage, // Keep for compatibility
            status = "running", // Frontend expects string status during progress
            message = currentUrl ?? $"{completedUrls}/{totalUrls} URLs processed",
            totalUrls,
            completedUrls,
            currentUrl,
            timestamp = DateTime.UtcNow
        };

        // Broadcast real-time progress (matching HTML expectation)
        await Task.WhenAll(
            _hubContext.Clients.Group($"crawljob_{jobId}").SendAsync("CrawlJobProgressUpdate", progressData, cancellationToken),
            BroadcastToRelevantGroupsAsync(jobId, "CrawlJobProgressUpdate", progressData, cancellationToken)
        );
    }

    private async Task HandleJobStatusChangedAsync(JsonElement eventData, CancellationToken cancellationToken)
    {
        var jobId = eventData.GetProperty("jobId").GetGuid();
        var status = eventData.GetProperty("status").GetString();

        _logger.LogInformation("Job {JobId} status changed to {Status}", jobId, status);

        await _hubContext.Clients.Group($"crawljob_{jobId}")
            .SendAsync("CrawlJobStatusChanged", new { jobId, status, timestamp = DateTime.UtcNow }, cancellationToken);
    }

    private async Task HandleCrawlerFailedAsync(JsonElement eventData, CancellationToken cancellationToken)
    {
        var jobId = eventData.GetProperty("jobId").GetGuid();
        var error = eventData.TryGetProperty("errorMessage", out var errorProp) ? errorProp.GetString() : "Unknown error";

        _logger.LogWarning("Job {JobId} failed: {Error}", jobId, error);

        var failureData = new
        {
            jobId,
            error,
            timestamp = DateTime.UtcNow
        };

        // Broadcast failure (matching HTML expectation for 'CrawlFailed')
        await Task.WhenAll(
            _hubContext.Clients.Group($"crawljob_{jobId}").SendAsync("CrawlFailed", failureData, cancellationToken),
            BroadcastToRelevantGroupsAsync(jobId, "CrawlFailed", failureData, cancellationToken)
        );
    }

    private async Task HandleUrlCrawlStartedAsync(JsonElement eventData, CancellationToken cancellationToken)
    {
        var jobId = eventData.GetProperty("jobId").GetGuid();
        var url = eventData.GetProperty("url").GetString();

        await _hubContext.Clients.Group($"crawljob_{jobId}")
            .SendAsync("UrlCrawlStarted", new { jobId, url, timestamp = DateTime.UtcNow }, cancellationToken);
    }

    private async Task HandleUrlCrawlCompletedAsync(JsonElement eventData, CancellationToken cancellationToken)
    {
        var jobId = eventData.GetProperty("jobId").GetGuid();
        var url = eventData.GetProperty("url").GetString();

        await _hubContext.Clients.Group($"crawljob_{jobId}")
            .SendAsync("UrlCrawlCompleted", new { jobId, url, timestamp = DateTime.UtcNow }, cancellationToken);
    }

    private async Task HandleUrlCrawlFailedAsync(JsonElement eventData, CancellationToken cancellationToken)
    {
        var jobId = eventData.GetProperty("jobId").GetGuid();
        var url = eventData.GetProperty("url").GetString();
        var error = eventData.TryGetProperty("errorMessage", out var errorProp) ? errorProp.GetString() : "Unknown error";

        await _hubContext.Clients.Group($"crawljob_{jobId}")
            .SendAsync("UrlCrawlFailed", new { jobId, url, error, timestamp = DateTime.UtcNow }, cancellationToken);
    }

    private async Task HandleNavigationEventAsync(JsonElement eventData, CancellationToken cancellationToken)
    {
        var jobId = eventData.GetProperty("jobId").GetGuid();
        var eventType = eventData.TryGetProperty("eventType", out var typeProp) ? typeProp.GetString() : "Unknown";
        var message = eventData.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : null;
        var details = eventData.TryGetProperty("details", out var detailsProp) ? detailsProp.GetString() : null;
        
        // Optional properties for navigation steps
        int? stepNumber = eventData.TryGetProperty("stepNumber", out var stepProp) ? stepProp.GetInt32() : null;
        int? totalSteps = eventData.TryGetProperty("totalSteps", out var totalProp) ? totalProp.GetInt32() : null;
        int? pageNumber = eventData.TryGetProperty("pageNumber", out var pageProp) ? pageProp.GetInt32() : null;

        var eventPayload = new
        {
            jobId,
            eventType,
            message,
            details,
            stepNumber,
            totalSteps,
            pageNumber,
            timestamp = DateTime.UtcNow
        };

        await _hubContext.Clients.Group($"crawljob_{jobId}")
            .SendAsync("CrawlerDetailedEvent", eventPayload, cancellationToken);
    }

    private async Task HandleCrawl4AIJobCompletedAsync(JsonElement eventData, CancellationToken cancellationToken)
    {
        var jobId = eventData.GetProperty("jobId").GetGuid();
        var userId = eventData.TryGetProperty("userId", out var userProp) ? userProp.GetString() : "unknown";
        
        // Extract conversation_name from the "data" object (where Python puts it)
        string? conversationName = null;
        if (eventData.TryGetProperty("data", out var dataObj) && dataObj.ValueKind == JsonValueKind.Object)
        {
            conversationName = dataObj.TryGetProperty("conversation_name", out var nameProp) ? nameProp.GetString() : null;
        }
        
        _logger.LogInformation("Crawl4AI job {JobId} completed with conversation_name: {ConversationName}", jobId, conversationName ?? "<null>");
        
        // OPTIMIZED: Extract pre-generated embedding data from Python agent
        string? embeddingText = eventData.TryGetProperty("embedding_text", out var embedTextProp) ? embedTextProp.GetString() : null;
        string? schemaType = eventData.TryGetProperty("schema_type", out var schemaProp) ? schemaProp.GetString() : null;
        double? qualityScore = eventData.TryGetProperty("quality_score", out var qualityProp) ? qualityProp.GetDouble() : null;
        
        List<double>? embeddingVector = null;
        if (eventData.TryGetProperty("embedding_vector", out var vectorProp) && vectorProp.ValueKind == JsonValueKind.Array)
        {
            embeddingVector = vectorProp.EnumerateArray()
                .Select(e => e.GetDouble())
                .ToList();
        }

        _logger.LogInformation(
            "Crawl4AI job {JobId} completed with embedding: schema={Schema}, quality={Quality:F2}, embedding_dim={EmbedDim}",
            jobId, schemaType, qualityScore, embeddingVector?.Count ?? 0);

        // Extract other completion data
        var itemsCount = eventData.TryGetProperty("items_count", out var countProp) ? countProp.GetInt32() : 0;
        var finalUrl = eventData.TryGetProperty("final_url", out var urlProp) ? urlProp.GetString() : null;
        var executionTimeMs = eventData.TryGetProperty("execution_time_ms", out var timeProp) ? timeProp.GetDouble() : 0.0;
        var pagesCollected = eventData.TryGetProperty("pages_collected", out var pagesProp) ? pagesProp.GetInt32() : 1;

        // Extract the crawled data
        var extractedData = eventData.TryGetProperty("extracted_data", out var dataProp) 
            ? JsonSerializer.Serialize(dataProp) 
            : "[]";

        var completionData = new
        {
            jobId,
            userId,
            conversationName = conversationName,
            itemsCount,
            finalUrl,
            executionTimeMs,
            pagesCollected,
            extractedData,
            // OPTIMIZED: Include pre-generated embedding data
            embeddingText,
            embeddingVector,
            schemaType,
            qualityScore,
            status = "completed",
            timestamp = DateTime.UtcNow
        };

        // Broadcast to multiple groups and update chat message with results + embeddings
        await Task.WhenAll(
            _hubContext.Clients.Group($"crawljob_{jobId}").SendAsync("CrawlJobCompleted", completionData, cancellationToken),
            BroadcastToRelevantGroupsAsync(jobId, "CrawlJobCompleted", completionData, cancellationToken),
            UpdateChatMessageWithResultsAsync(jobId, conversationName, cancellationToken),
            StoreNormalizedDataWithEmbeddingsAsync(jobId, extractedData, embeddingText, embeddingVector, schemaType, qualityScore, cancellationToken)
        );
    }

    private async Task StoreNormalizedDataWithEmbeddingsAsync(
        Guid jobId, 
        string extractedDataJson, 
        string? embeddingText, 
        List<double>? embeddingVector,
        string? schemaType,
        double? qualityScore,
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            // Find the conversation ID for this job
            var chatMessage = await unitOfWork.CrawlerChatMessages.GetByCrawlJobIdAsync(jobId, cancellationToken);
            if (chatMessage == null)
            {
                _logger.LogWarning("No chat message found for job {JobId}, skipping normalized data storage", jobId);
                return;
            }

            // OPTIMIZED: Use pre-generated embedding data from Python agent (no API call needed!)
            var conversationCrawlData = new ConversationCrawlData
            {
                ConversationId = chatMessage.ConversationId,
                CrawlJobId = jobId,
                NormalizedDataJson = extractedDataJson, // Already in JSON format
                EmbeddingText = embeddingText ?? string.Empty,
                VectorEmbeddingJson = embeddingVector != null 
                    ? JsonSerializer.Serialize(embeddingVector) 
                    : null,
                DetectedSchemaJson = JsonSerializer.Serialize(new 
                { 
                    type = schemaType ?? "generic_data",
                    quality_score = qualityScore ?? 0.5
                }),
                DataQualityScore = qualityScore ?? 0.5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await unitOfWork.ConversationCrawlData.AddAsync(conversationCrawlData, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("OPTIMIZED: Stored normalized crawl data for job {JobId} with pre-generated embeddings (no API call!)", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store normalized data for job {JobId}", jobId);
            // Don't throw - this is a background operation
        }
    }

    // ========== Helper Methods ==========

    private async Task BroadcastToRelevantGroupsAsync(Guid jobId, string eventName, object data, CancellationToken cancellationToken)
    {
        try
        {
            // Create scope to access scoped services (this is a singleton BackgroundService)
            using var scope = _serviceScopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            // Query chat message by CrawlJobId to get conversation/group/assignment context
            var chatMessage = await unitOfWork.CrawlerChatMessages.GetByCrawlJobIdAsync(jobId, cancellationToken);

            if (chatMessage == null)
            {
                _logger.LogInformation("No chat message found for job {JobId}, skipping group broadcasts (not all crawls are from chat)", jobId);
                return;
            }

            var tasks = new List<Task>();

            // Broadcast to conversation (always exists for chat-initiated crawls)
            tasks.Add(_hubContext.Clients
                .Group($"conversation_{chatMessage.ConversationId}")
                .SendAsync(eventName, data, cancellationToken));

            // Broadcast to group workspace (if group crawl)
            if (chatMessage.GroupId.HasValue)
            {
                tasks.Add(_hubContext.Clients
                    .Group($"group_{chatMessage.GroupId}")
                    .SendAsync(eventName, data, cancellationToken));
            }

            // Broadcast to assignment (if assignment-wide visibility)
            if (chatMessage.AssignmentId != Guid.Empty)
            {
                tasks.Add(_hubContext.Clients
                    .Group($"assignment_{chatMessage.AssignmentId}")
                    .SendAsync(eventName, data, cancellationToken));
            }

            await Task.WhenAll(tasks);

            _logger.LogInformation("Broadcasted {EventName} for job {JobId} to conversation {ConversationId}",
                eventName, jobId, chatMessage.ConversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting {EventName} for job {JobId} to relevant groups", eventName, jobId);
            // Don't rethrow - broadcast failures shouldn't crash the consumer
        }
    }

    private async Task UpdateChatMessageWithResultsAsync(Guid jobId, string? conversationName, CancellationToken cancellationToken)
    {
        try
        {
            // Create scope to access scoped services
            using var scope = _serviceScopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var crawlerService = scope.ServiceProvider.GetRequiredService<ICrawlerIntegrationService>();
            var normalizationService = scope.ServiceProvider.GetRequiredService<ICrawlDataNormalizationService>();

            // Get the chat message associated with this crawl job
            var chatMessage = await unitOfWork.CrawlerChatMessages.GetByCrawlJobIdAsync(jobId, cancellationToken);

            if (chatMessage == null)
            {
                _logger.LogInformation("No chat message found for job {JobId}, skipping result update", jobId);
                return;
            }

            // Fetch crawl summary from WebCrawlerService
            var summary = await crawlerService.GetCrawlSummaryAsync(jobId, cancellationToken);

            // Update the message with results
            chatMessage.CrawlResultSummary = summary;
            chatMessage.MessageType = MessageType.CrawlResult;
            chatMessage.EditedAt = DateTime.UtcNow;

            // Save the crawl message update early so later steps can't block the status change
            await unitOfWork.CrawlerChatMessages.UpdateAsync(chatMessage);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Saved crawl message update for job {JobId}", jobId);

            var conversationUpdated = false;
            try
            {
                // Update conversation name in Conversations table
                // Priority 1: Get from Kafka event data
                _logger.LogInformation("🔍 UpdateChatMessage: conversationName from Kafka = '{Name}' (null: {IsNull})", 
                    conversationName ?? "<NULL>", string.IsNullOrEmpty(conversationName));
                
                if (!string.IsNullOrEmpty(conversationName))
                {
                    var conversation = await unitOfWork.Conversations.GetByIdAsync(chatMessage.ConversationId, cancellationToken);
                    var isNewConversation = conversation == null;
                    _logger.LogInformation("🔍 GetByIdAsync result: conversation {IsNull}, ConversationId = {ConversationId}", 
                        conversation == null ? "IS NULL" : "EXISTS", chatMessage.ConversationId);
                    
                    if (conversation == null)
                    {
                        // Conversation doesn't exist - this can happen if chat was initiated without proper setup
                        // Create a minimal conversation record so we can set the name
                        _logger.LogWarning("⚠️ Conversation {ConversationId} does not exist, creating it now", 
                            chatMessage.ConversationId);
                        
                        // Get CourseId from Assignment
                        var assignment = await unitOfWork.Assignments.GetByIdAsync(chatMessage.AssignmentId, cancellationToken);
                        var courseId = assignment?.CourseId ?? Guid.Empty;
                        
                        if (courseId == Guid.Empty)
                        {
                            _logger.LogWarning("⚠️ Could not find CourseId for Assignment {AssignmentId}, using Empty GUID", 
                                chatMessage.AssignmentId);
                        }
                        
                        conversation = new Conversation
                        {
                            Id = chatMessage.ConversationId,
                            Name = conversationName,
                            CourseId = courseId,
                            User1Id = chatMessage.SenderId,
                            User2Id = Guid.Empty, // System/bot conversation
                            IsCrawler = true,
                            CreatedAt = DateTime.UtcNow,
                            LastMessageAt = DateTime.UtcNow,
                            CreatedBy = chatMessage.SenderId
                        };
                        
                        await unitOfWork.Conversations.AddAsync(conversation);
                        conversationUpdated = true;
                        _logger.LogInformation("✅ Created conversation {ConversationId} with name: {Name} and CourseId: {CourseId}", 
                            chatMessage.ConversationId, conversationName, courseId);
                    }
                    else
                    {
                        _logger.LogInformation("🔍 Found conversation {ConversationId}, current Name = '{CurrentName}' (null: {IsNull})", 
                            chatMessage.ConversationId, conversation.Name ?? "<NULL>", string.IsNullOrEmpty(conversation.Name));
                    }
                    
                    if (!isNewConversation && conversation != null && string.IsNullOrEmpty(conversation.Name))
                    {
                        _logger.LogInformation("🔧 Setting conversation.Name = '{Name}'", conversationName);
                        conversation.Name = conversationName;
                        await unitOfWork.Conversations.UpdateAsync(conversation);
                        conversationUpdated = true;
                        _logger.LogInformation("✅ Updated conversation {ConversationId} with name from Kafka: {Name}", 
                            chatMessage.ConversationId, conversationName);
                    }
                    else if (!isNewConversation && conversation != null && !string.IsNullOrEmpty(conversation.Name))
                    {
                        _logger.LogInformation("⏭️ Skipped update - conversation {ConversationId} already has name: '{ExistingName}'", 
                            chatMessage.ConversationId, conversation.Name);
                    }
                    else if (!isNewConversation && conversation == null)
                    {
                        _logger.LogWarning("⚠️ Conversation {ConversationId} not found for update", chatMessage.ConversationId);
                    }
                }
                // Priority 2: Fetch from WebCrawlerService API if Kafka didn't have it
                else
                {
                    try
                    {
                        var jobDetails = await crawlerService.GetCrawlJobAsync(jobId, cancellationToken);
                        if (jobDetails != null && !string.IsNullOrEmpty(jobDetails.ConversationName))
                        {
                            var conversation = await unitOfWork.Conversations.GetByIdAsync(chatMessage.ConversationId, cancellationToken);
                            if (conversation == null)
                            {
                                var assignment = await unitOfWork.Assignments.GetByIdAsync(chatMessage.AssignmentId, cancellationToken);
                                var courseId = assignment?.CourseId ?? Guid.Empty;

                                if (courseId == Guid.Empty)
                                {
                                    _logger.LogWarning("Could not find CourseId for Assignment {AssignmentId}, using Empty GUID", 
                                        chatMessage.AssignmentId);
                                }

                                conversation = new Conversation
                                {
                                    Id = chatMessage.ConversationId,
                                    Name = jobDetails.ConversationName,
                                    CourseId = courseId,
                                    User1Id = chatMessage.SenderId,
                                    User2Id = Guid.Empty, // System/bot conversation
                                    IsCrawler = true,
                                    CreatedAt = DateTime.UtcNow,
                                    LastMessageAt = DateTime.UtcNow,
                                    CreatedBy = chatMessage.SenderId
                                };

                                await unitOfWork.Conversations.AddAsync(conversation);
                                conversationUpdated = true;
                                _logger.LogInformation("Created conversation {ConversationId} with name from API: {Name}", 
                                    chatMessage.ConversationId, jobDetails.ConversationName);
                            }
                            else if (string.IsNullOrEmpty(conversation.Name))
                            {
                                conversation.Name = jobDetails.ConversationName;
                                await unitOfWork.Conversations.UpdateAsync(conversation);
                                conversationUpdated = true;
                                _logger.LogInformation("Updated conversation {ConversationId} with name from API: {Name}", 
                                    chatMessage.ConversationId, jobDetails.ConversationName);
                            }
                            else
                            {
                                _logger.LogInformation("Skipped update - conversation {ConversationId} already has name: '{ExistingName}'", 
                                    chatMessage.ConversationId, conversation.Name);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("No conversation name available for job {JobId}", jobId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to fetch conversation name from WebCrawlerService for job {JobId}", jobId);
                    }
                }

                if (conversationUpdated)
                {
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update conversation metadata for job {JobId}", jobId);
            }

            // Check if normalized data already exists (from Crawl4AI optimized path)
            // Note: Crawl4AI jobs already store normalized data via HandleCrawl4AIJobCompletedAsync
            // This normalization fallback is only for legacy crawls
            try
            {
                var existingData = await unitOfWork.ConversationCrawlData
                    .GetByCrawlJobIdAsync(jobId, cancellationToken);

                if (existingData == null)
                {
                    // Trigger data normalization for RAG in background (don't block)
                    // This is for legacy crawls that don't include pre-normalized data
                    _logger.LogInformation("No pre-normalized data found for job {JobId}, triggering normalization", jobId);
                    
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            _logger.LogInformation("Starting data normalization for conversation {ConversationId}, job {JobId}",
                                chatMessage.ConversationId, jobId);
                            
                            await normalizationService.NormalizeAndStoreAsync(
                                chatMessage.ConversationId,
                                jobId,
                                CancellationToken.None);
                            
                            _logger.LogInformation("Data normalization completed for job {JobId}", jobId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to normalize crawl data for job {JobId}", jobId);
                            // Don't crash - normalization failures are non-critical
                        }
                    }, cancellationToken);
                }
                else
                {
                    _logger.LogInformation("✅ Skipping normalization for job {JobId} - data already normalized by Crawl4AI agent", jobId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not check for existing normalized data for job {JobId}. " +
                    "This is expected if database schema is being updated. Skipping normalization.", jobId);
                // Skip normalization if check fails - data is likely already stored via Kafka
            }

            // Broadcast result ready notification
            var resultReadyData = new
            {
                messageId = chatMessage.Id,
                jobId,
                summary,
                timestamp = DateTime.UtcNow
            };

            var broadcastTasks = new List<Task>
            {
                _hubContext.Clients
                    .Group($"conversation_{chatMessage.ConversationId}")
                    .SendAsync("CrawlResultReady", resultReadyData, cancellationToken)
            };

            if (chatMessage.GroupId.HasValue)
            {
                broadcastTasks.Add(_hubContext.Clients
                    .Group($"group_{chatMessage.GroupId}")
                    .SendAsync("CrawlResultReady", resultReadyData, cancellationToken));
            }

            await Task.WhenAll(broadcastTasks);

            _logger.LogInformation("Updated chat message {MessageId} with crawl results for job {JobId}",
                chatMessage.Id, jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating chat message with results for job {JobId}", jobId);
            // Don't rethrow - result update failures shouldn't crash the consumer
        }
    }

    private static string? GetHeaderValue(Headers headers, string key)
    {
        var header = headers.FirstOrDefault(h => h.Key == key);
        return header != null ? System.Text.Encoding.UTF8.GetString(header.GetValueBytes()) : null;
    }

    public override void Dispose()
    {
        _consumer?.Dispose();
        base.Dispose();
    }
}

/// <summary>
/// Configuration settings for Kafka consumer in ClassroomService
/// </summary>
public class KafkaConsumerSettings
{
    public const string SectionName = "KafkaSettings";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string ConsumerGroupId { get; init; } = "classroom-service-group";

    // Topics to subscribe to (should match WebCrawlerService publisher)
    public string JobStartedTopic { get; init; } = "crawler.job.started";
    public string JobCompletedTopic { get; init; } = "crawler.job.completed";
    public string JobProgressTopic { get; init; } = "crawler.job.progress";
    public string JobStatusChangedTopic { get; init; } = "crawler.job.status-changed";
    public string CrawlerFailedTopic { get; init; } = "crawler.job.failed";
    public string UrlCrawlStartedTopic { get; init; } = "crawler.url.started";
    public string UrlCrawlCompletedTopic { get; init; } = "crawler.url.completed";
    public string UrlCrawlFailedTopic { get; init; } = "crawler.url.failed";
}
