using System.Linq;
using System.Text.Json;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Enums;
using ClassroomService.Application.Features.CrawlerChat.Commands;
using ClassroomService.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;
using MediatR;

namespace ClassroomService.Application.Hubs;

/// <summary>
/// SignalR hub for interactive chat-based crawler communication
/// Supports group collaboration and real-time updates
/// </summary>
public class CrawlerChatHub : Hub
{
    private readonly ILogger<CrawlerChatHub> _logger;
    private readonly ICrawlerIntegrationService _crawlerService;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private const string DefaultNoDataResponse = "I cannot find that information in the crawled data. Please ask another question.";
    private const string AgentDisplayName = "Crawler Agent";

    public CrawlerChatHub(
        ILogger<CrawlerChatHub> logger,
        ICrawlerIntegrationService crawlerService,
        IMediator mediator,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _logger = logger;
        _crawlerService = crawlerService;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    // ========== Connection Management ==========

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client {ConnectionId} connected to CrawlerChatHub", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogWarning(exception, "Client {ConnectionId} disconnected with error", Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("Client {ConnectionId} disconnected", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Get current connection ID for client reference
    /// </summary>
    public string GetConnectionId()
    {
        return Context.ConnectionId;
    }

    // ========== Conversation Thread Management ==========

    /// <summary>
    /// Join a conversation thread to receive updates
    /// </summary>
    /// <param name="conversationId">Conversation thread ID</param>
    public async Task JoinConversation(string conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
        _logger.LogInformation("Client {ConnectionId} joined conversation {ConversationId}",
            Context.ConnectionId, conversationId);

        await Clients.Caller.SendAsync("ConversationJoined", conversationId);
    }

    /// <summary>
    /// Leave a conversation thread
    /// </summary>
    /// <param name="conversationId">Conversation thread ID</param>
    public async Task LeaveConversation(string conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
        _logger.LogInformation("Client {ConnectionId} left conversation {ConversationId}",
            Context.ConnectionId, conversationId);

        await Clients.Caller.SendAsync("ConversationLeft", conversationId);
    }

    // ========== Group Collaboration ==========

    /// <summary>
    /// Join a group's collaborative workspace
    /// </summary>
    /// <param name="groupId">Group ID</param>
    public async Task JoinGroupWorkspace(string groupId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"group_{groupId}");
        _logger.LogInformation("Client {ConnectionId} joined group workspace {GroupId}",
            Context.ConnectionId, groupId);

        await Clients.Caller.SendAsync("GroupWorkspaceJoined", groupId);
    }

    /// <summary>
    /// Leave a group's collaborative workspace
    /// </summary>
    /// <param name="groupId">Group ID</param>
    public async Task LeaveGroupWorkspace(string groupId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"group_{groupId}");
        _logger.LogInformation("Client {ConnectionId} left group workspace {GroupId}",
            Context.ConnectionId, groupId);

        await Clients.Caller.SendAsync("GroupWorkspaceLeft", groupId);
    }

    // ========== Assignment Collaboration ==========

    /// <summary>
    /// Subscribe to assignment-wide crawler activity
    /// </summary>
    /// <param name="assignmentId">Assignment ID</param>
    public async Task SubscribeToAssignment(string assignmentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"assignment_{assignmentId}");
        _logger.LogInformation("Client {ConnectionId} subscribed to assignment {AssignmentId}",
            Context.ConnectionId, assignmentId);

        await Clients.Caller.SendAsync("AssignmentSubscribed", assignmentId);
    }

    /// <summary>
    /// Unsubscribe from assignment updates
    /// </summary>
    /// <param name="assignmentId">Assignment ID</param>
    public async Task UnsubscribeFromAssignment(string assignmentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"assignment_{assignmentId}");
        _logger.LogInformation("Client {ConnectionId} unsubscribed from assignment {AssignmentId}",
            Context.ConnectionId, assignmentId);

        await Clients.Caller.SendAsync("AssignmentUnsubscribed", assignmentId);
    }

    // ========== Chat Message Handling ==========

    /// <summary>
    /// Send a chat message to the crawler
    /// </summary>
    /// <param name="message">Chat message content</param>
    public async Task SendCrawlerMessage(ChatMessageDto message)
    {
        try
        {
            _logger.LogInformation("Processing crawler message from {UserId} in conversation {ConversationId}, Type: {MessageType}",
                message.UserId, message.ConversationId, message.MessageType);

            // Detect CrawlRequest messages and process via MediatR command
            if (message.MessageType == MessageType.CrawlRequest)
            {
                var command = new InitiateCrawlFromChatCommand
                {
                    MessageContent = message.Content,
                    ConversationId = message.ConversationId,
                    SenderId = message.UserId,
                    SenderName = message.UserName,
                    AssignmentId = message.AssignmentId,
                    GroupId = message.GroupId,
                    MessageType = MessageType.CrawlRequest,
                    MaxPages = message.MaxPages // Pass through optional max pages from frontend
                };

                var result = await _mediator.Send(command);

                if (result.Success)
                {
                    // Update message with crawl job ID
                    message.CrawlJobId = result.CrawlJobId;

                    // Auto-subscribe caller to job updates (so quota errors are delivered)
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"crawljob_{result.CrawlJobId}");

                    // Broadcast to conversation participants
                    await Clients.Group($"conversation_{message.ConversationId}")
                        .SendAsync("UserMessageReceived", message);

                    if (message.GroupId.HasValue)
                    {
                        await Clients.Group($"group_{message.GroupId}")
                            .SendAsync("GroupMessageReceived", message);
                    }

                    // Send acknowledgment with job ID to sender
                    await Clients.Caller.SendAsync("CrawlInitiated", new
                    {
                        messageId = result.MessageId,
                        crawlJobId = result.CrawlJobId,
                        message = result.Message,
                        success = true
                    });

                    _logger.LogInformation("Crawl initiated from chat: MessageId {MessageId}, JobId {JobId}",
                        result.MessageId, result.CrawlJobId);
                }
                else
                {
                    // Send error notification to sender
                    await Clients.Caller.SendAsync("CrawlFailed", new
                    {
                        messageId = message.MessageId,
                        error = result.Message,
                        success = false
                    });

                    _logger.LogWarning("Failed to initiate crawl from chat: {Error}", result.Message);
                }
            }
            else
            {
                // Regular message - persist to database then broadcast
                var chatMessage = new CrawlerChatMessage
                {
                    Id = Guid.NewGuid(),
                    ConversationId = message.ConversationId,
                    AssignmentId = message.AssignmentId ?? Guid.Empty,
                    GroupId = message.GroupId,
                    SenderId = message.UserId,
                    MessageContent = message.Content,
                    MessageType = message.MessageType,
                    IsSystemMessage = false,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = message.UserId
                };

                await _unitOfWork.CrawlerChatMessages.AddAsync(chatMessage);
                await _unitOfWork.SaveChangesAsync();

                // Update message DTO with persisted ID and timestamp
                message.MessageId = chatMessage.Id;
                message.Timestamp = chatMessage.CreatedAt;

                // Broadcast to conversation participants
                await Clients.Group($"conversation_{message.ConversationId}")
                    .SendAsync("UserMessageReceived", message);

                if (message.GroupId.HasValue)
                {
                    await Clients.Group($"group_{message.GroupId}")
                        .SendAsync("GroupMessageReceived", message);
                }

                // Acknowledge receipt to sender with message ID
                await Clients.Caller.SendAsync("MessageSent", new
                {
                    messageId = message.MessageId,
                    timestamp = message.Timestamp,
                    success = true
                });

                _logger.LogInformation("Regular message persisted: MessageId {MessageId}, Conversation {ConversationId}",
                    message.MessageId, message.ConversationId);

                await TrySendAgentResponseAsync(message, Context.ConnectionAborted);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing crawler message");
            await Clients.Caller.SendAsync("MessageError", message.MessageId, ex.Message);
        }
    }

    /// <summary>
    /// Broadcast crawler response to conversation participants
    /// </summary>
    /// <param name="response">Crawler response message</param>
    public async Task BroadcastCrawlerResponse(CrawlerResponseDto response)
    {
        try
        {
            _logger.LogInformation("Broadcasting crawler response for job {JobId} to conversation {ConversationId}",
                response.CrawlJobId, response.ConversationId);

            // Broadcast to conversation group
            await Clients.Group($"conversation_{response.ConversationId}")
                .SendAsync("CrawlerResponseReceived", response);

            // If it's a group conversation, also broadcast to group workspace
            if (response.GroupId.HasValue)
            {
                await Clients.Group($"group_{response.GroupId}")
                    .SendAsync("GroupCrawlerResponse", response);
            }

            // If it's an assignment, broadcast to assignment subscribers
            if (response.AssignmentId.HasValue)
            {
                await Clients.Group($"assignment_{response.AssignmentId}")
                    .SendAsync("AssignmentCrawlerResponse", response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting crawler response");
        }
    }

    // ========== Typing Indicators ==========

    /// <summary>
    /// Notify conversation participants that user is typing
    /// </summary>
    /// <param name="conversationId">Conversation ID</param>
    /// <param name="userId">User ID who is typing</param>
    /// <param name="userName">User's display name</param>
    public async Task UserTyping(string conversationId, string userId, string userName)
    {
        await Clients.OthersInGroup($"conversation_{conversationId}")
            .SendAsync("UserTyping", conversationId, userId, userName);
    }

    /// <summary>
    /// Notify conversation participants that user stopped typing
    /// </summary>
    /// <param name="conversationId">Conversation ID</param>
    /// <param name="userId">User ID who stopped typing</param>
    public async Task UserStoppedTyping(string conversationId, string userId)
    {
        await Clients.OthersInGroup($"conversation_{conversationId}")
            .SendAsync("UserStoppedTyping", conversationId, userId);
    }

    // ========== Crawl Job Status Updates ==========

    /// <summary>
    /// Subscribe to real-time crawl job status for a conversation
    /// </summary>
    /// <param name="jobId">Crawl job ID</param>
    /// <param name="conversationId">Conversation ID</param>
    public async Task SubscribeToCrawlJob(string jobId, string conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"crawljob_{jobId}");
        _logger.LogInformation("Client {ConnectionId} subscribed to crawl job {JobId} for conversation {ConversationId}",
            Context.ConnectionId, jobId, conversationId);

        // Try to get initial job status
        if (Guid.TryParse(jobId, out var guid))
        {
            try
            {
                var userId = _currentUserService.UserId ?? Guid.Empty;
                var status = await _crawlerService.GetCrawlStatusAsync(guid, userId);
                await Clients.Caller.SendAsync("CrawlJobStatus", status);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get initial crawl job status for {JobId}", jobId);
            }
        }
    }

    /// <summary>
    /// Unsubscribe from crawl job updates
    /// </summary>
    /// <param name="jobId">Crawl job ID</param>
    public async Task UnsubscribeFromCrawlJob(string jobId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"crawljob_{jobId}");
        _logger.LogInformation("Client {ConnectionId} unsubscribed from crawl job {JobId}",
            Context.ConnectionId, jobId);
    }

    /// <summary>
    /// Broadcast crawl job progress update to subscribers
    /// </summary>
    /// <param name="jobId">Crawl job ID</param>
    /// <param name="status">Job status update</param>
    public async Task BroadcastJobProgress(string jobId, CrawlJobStatusResponse status)
    {
        await Clients.Group($"crawljob_{jobId}")
            .SendAsync("CrawlJobProgressUpdate", status);
    }

    private async Task TrySendAgentResponseAsync(ChatMessageDto triggerMessage, CancellationToken cancellationToken)
    {
        try
        {
            var content = triggerMessage.Content?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(content))
            {
                return;
            }

            // Send all queries through RAG path - Python SmartRAG handles classification
            var answer = await _crawlerService.AskQuestionAsync(triggerMessage.ConversationId, content, cancellationToken);
            
            if (string.IsNullOrWhiteSpace(answer))
            {
                await SendAgentMessageAsync(triggerMessage, DefaultNoDataResponse, MessageType.AiSummary, cancellationToken);
                return;
            }

            // Check if answer contains chart JSON from Python agent
            if (TryExtractPythonChartJson(answer, out var chartExtracted))
            {
                var summaryText = !string.IsNullOrWhiteSpace(chartExtracted.SummaryText)
                    ? chartExtracted.SummaryText!.Trim()
                    : "Đã tạo biểu đồ từ dữ liệu crawl.";

                var metadataJson = BuildApexVisualizationWrapperJson(
                    latestResultCrawlJobId: null,
                    insightHighlights: chartExtracted.InsightHighlights,
                    chartTitle: chartExtracted.ChartTitle,
                    chartType: chartExtracted.ChartType,
                    chartData: chartExtracted.ChartData);

                await SendAgentMessageAsync(
                    triggerMessage,
                    BuildAgentSummaryResponse(summaryText),
                    MessageType.AiSummary,
                    cancellationToken,
                    crawlJobId: null,
                    metadataJson: metadataJson);
            }
            else
            {
                await SendAgentMessageAsync(triggerMessage, answer, MessageType.AiSummary, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate agent response for conversation {ConversationId}",
                triggerMessage.ConversationId);
        }
    }

    // --- Helper methods ---

    private static string BuildApexVisualizationWrapperJson(
        Guid? latestResultCrawlJobId,
        IEnumerable<string>? insightHighlights,
        string? chartTitle,
        string? chartType,
        object? chartData)
    {
        var type = (chartType ?? "pie").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(type)) type = "pie";
        var normalized = NormalizeChartData(chartData);
        object dataNode;
        if (type is "pie" or "donut" or "radialbar" or "polararea")
        {
            dataNode = new
            {
                chart = new { type, title = chartTitle ?? "Chart" },
                labels = normalized.Labels,
                series = normalized.SeriesNumbers
            };
        }
        else
        {
            var seriesObj = normalized.SeriesObjects.Length > 0
                ? normalized.SeriesObjects
                : new[] { new { name = "Data", data = normalized.SeriesNumbers.Cast<object>().ToArray() } };
            dataNode = new
            {
                chart = new { type, title = chartTitle ?? "Chart" },
                categories = normalized.Categories.Length > 0 ? normalized.Categories : normalized.Labels,
                series = seriesObj
            };
        }
        var wrapper = new
        {
            latestResultCrawlJobId = latestResultCrawlJobId,
            insightHighlights = insightHighlights ?? Array.Empty<string>(),
            visualizationData = new
            {
                type,
                data = dataNode,
                options = new
                {
                    plugins = new
                    {
                        legend = new { position = "bottom" }
                    }
                }
            }
        };
        return JsonSerializer.Serialize(wrapper);
    }
    private sealed class NormalizedChart
    {
        public string[] Labels { get; set; } = Array.Empty<string>();
        public string[] Categories { get; set; } = Array.Empty<string>();
        public double[] SeriesNumbers { get; set; } = Array.Empty<double>();
        public object[] SeriesObjects { get; set; } = Array.Empty<object>();
    }
    private static NormalizedChart NormalizeChartData(object? chartData)
    {
        var result = new NormalizedChart();
        if (chartData == null) return result;
        try
        {
            var json = JsonSerializer.Serialize(chartData);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return result;
            if (root.TryGetProperty("categories", out var catEl) && catEl.ValueKind == JsonValueKind.Array)
                result.Categories = ReadStringArray(catEl);
            if (root.TryGetProperty("labels", out var labelsEl) && labelsEl.ValueKind == JsonValueKind.Array)
                result.Labels = ReadStringArray(labelsEl);
            if (root.TryGetProperty("series", out var seriesEl))
            {
                if (seriesEl.ValueKind == JsonValueKind.Array)
                {
                    var first = seriesEl.EnumerateArray().FirstOrDefault();
                    if (first.ValueKind == JsonValueKind.Number || first.ValueKind == JsonValueKind.String)
                    {
                        result.SeriesNumbers = ReadNumberArray(seriesEl);
                    }
                    else if (first.ValueKind == JsonValueKind.Object)
                    {
                        var list = new List<object>();
                        foreach (var s in seriesEl.EnumerateArray())
                        {
                            if (s.ValueKind != JsonValueKind.Object) continue;
                            string name = "Series";
                            if (s.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String)
                                name = n.GetString() ?? "Series";
                            object[] data = Array.Empty<object>();
                            if (s.TryGetProperty("data", out var d) && d.ValueKind == JsonValueKind.Array)
                            {
                                data = d.EnumerateArray()
                                    .Select(x => x.ValueKind == JsonValueKind.Number && x.TryGetDouble(out var dd) ? (object)dd : (object)x.ToString())
                                    .ToArray();
                            }
                            list.Add(new { name, data });
                        }
                        result.SeriesObjects = list.ToArray();
                    }
                }
                return result;
            }
            if (root.TryGetProperty("datasets", out var datasetsEl) && datasetsEl.ValueKind == JsonValueKind.Array)
            {
                if (result.Labels.Length == 0 && root.TryGetProperty("labels", out var cjLabelsEl) && cjLabelsEl.ValueKind == JsonValueKind.Array)
                    result.Labels = ReadStringArray(cjLabelsEl);
                var firstDs = datasetsEl.EnumerateArray().FirstOrDefault();
                if (firstDs.ValueKind == JsonValueKind.Object &&
                    firstDs.TryGetProperty("data", out var dataEl) &&
                    dataEl.ValueKind == JsonValueKind.Array)
                {
                    result.SeriesNumbers = ReadNumberArray(dataEl);
                    return result;
                }
            }
            if (root.TryGetProperty("data", out var nested) && nested.ValueKind == JsonValueKind.Object)
            {
                var nestedObj = JsonSerializer.Deserialize<object>(nested.GetRawText());
                return NormalizeChartData(nestedObj);
            }
        }
        catch
        {
            // ignore
        }
        return result;
    }
    private static double[] ReadNumberArray(JsonElement arr)
    {
        var list = new List<double>();
        foreach (var el in arr.EnumerateArray())
        {
            if (el.ValueKind == JsonValueKind.Number && el.TryGetDouble(out var d))
                list.Add(d);
            else if (el.ValueKind == JsonValueKind.String && double.TryParse(el.GetString(), out var ds))
                list.Add(ds);
        }
        return list.ToArray();
    }
    private static string[] ReadStringArray(JsonElement arr)
    {
        var list = new List<string>();
        foreach (var el in arr.EnumerateArray())
        {
            if (el.ValueKind == JsonValueKind.String)
                list.Add(el.GetString() ?? "");
            else
                list.Add(el.ToString());
        }
        return list.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
    }
    private sealed class ExtractedChartPayload
    {
        public string? SummaryText { get; set; }
        public List<string> InsightHighlights { get; set; } = new();
        public string? ChartTitle { get; set; }
        public string? ChartType { get; set; }
        public object? ChartData { get; set; }
    }

    /// <summary>
    /// Extract chart JSON from Python agent response.
    /// Python agent returns format: {"chart_type": "bar/pie/line", "data": [...], "labels": [...]}
    /// </summary>
    private bool TryExtractPythonChartJson(string? answerText, out ExtractedChartPayload payload)
    {
        payload = new ExtractedChartPayload();
        if (string.IsNullOrWhiteSpace(answerText))
        {
            _logger.LogDebug("TryExtractPythonChartJson: Input is null or whitespace");
            return false;
        }

        try
        {
            var text = answerText.Trim();
            string? summaryBeforeBlock = null;
            
            _logger.LogDebug("TryExtractPythonChartJson: Processing text length={Length}, contains ```={HasCodeBlock}", 
                text.Length, text.Contains("```"));
            
            // Extract JSON from markdown code block if present
            if (text.Contains("```"))
            {
                // Find the start of code block
                var fenceStart = text.IndexOf("```");
                if (fenceStart > 0)
                {
                    // Text before code block is the summary
                    summaryBeforeBlock = text.Substring(0, fenceStart).Trim();
                    _logger.LogDebug("TryExtractPythonChartJson: Found summary before code block, length={SummaryLength}", 
                        summaryBeforeBlock.Length);
                }
                
                // Extract content between ``` markers
                var match = System.Text.RegularExpressions.Regex.Match(
                    text, 
                    @"```(?:json)?\s*([\s\S]*?)```",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                if (match.Success)
                {
                    text = match.Groups[1].Value.Trim();
                    _logger.LogDebug("TryExtractPythonChartJson: Extracted from code block, new length={Length}, starts with={Preview}", 
                        text.Length, text.Substring(0, Math.Min(50, text.Length)));
                }
                else
                {
                    _logger.LogWarning("TryExtractPythonChartJson: Regex failed to match code block pattern");
                    return false;
                }
            }
            
            // After extraction, text should start with { if it's JSON
            // Try to parse it directly instead of searching for patterns
            var jsonText = text.Trim();
            
            // Check if it looks like JSON
            if (!jsonText.StartsWith("{"))
            {
                _logger.LogDebug("TryExtractPythonChartJson: Text doesn't start with {{ after extraction, starts with={Start}", 
                    jsonText.Substring(0, Math.Min(20, jsonText.Length)));
                return false;
            }

            // Extract summary if there was text before the code block
            if (!string.IsNullOrWhiteSpace(summaryBeforeBlock))
            {
                payload.SummaryText = summaryBeforeBlock;
            }

            // Find the end of JSON object using brace matching to avoid including trailing text
            int braceCount = 0;
            int jsonEnd = -1;
            for (int i = 0; i < jsonText.Length; i++)
            {
                if (jsonText[i] == '{') braceCount++;
                else if (jsonText[i] == '}')
                {
                    braceCount--;
                    if (braceCount == 0)
                    {
                        jsonEnd = i;
                        break; // Found the matching closing brace
                    }
                }
            }

            if (jsonEnd < 0)
            {
                _logger.LogWarning("TryExtractPythonChartJson: No matching closing brace found");
                return false;
            }

            jsonText = jsonText.Substring(0, jsonEnd + 1).Trim();
            _logger.LogDebug("TryExtractPythonChartJson: JSON after brace matching, length={Length}, preview={Preview}", 
                jsonText.Length, jsonText.Substring(0, Math.Min(100, jsonText.Length)));

            // Convert Python dict syntax to JSON if needed (single quotes to double quotes)
            // Only convert quotes that are string delimiters, not apostrophes inside strings
            if (jsonText.Contains("'"))
            {
                // Simple heuristic: if it starts with {'  it's likely Python dict syntax
                if (jsonText.StartsWith("{'") || jsonText.Contains(": '"))
                {
                    _logger.LogDebug("TryExtractPythonChartJson: Detected Python dict syntax, converting to JSON");
                    // Use regex to replace single quotes around keys and values
                    // This handles: 'key': 'value' -> "key": "value"
                    // But preserves: "Juve'Heal" (apostrophes inside strings)
                    jsonText = System.Text.RegularExpressions.Regex.Replace(
                        jsonText,
                        @"'([^']*(?:''[^']*)*)'", // Match 'text' including escaped ''
                        m => "\"" + m.Groups[1].Value.Replace("''", "'") + "\"");
                }
            }

            using var doc = JsonDocument.Parse(jsonText, new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });
            var root = doc.RootElement;

            // Check if this is new structured format: {"title": "...", "insightHighlights": [...], "content": "...", "chart": {...}}
            if (root.TryGetProperty("chart", out var chartEl) && chartEl.ValueKind == JsonValueKind.Object)
            {
                _logger.LogDebug("TryExtractPythonChartJson: Detected new structured format with title/insights");
                
                // Extract title
                if (root.TryGetProperty("title", out var titleEl) && titleEl.ValueKind == JsonValueKind.String)
                {
                    payload.ChartTitle = titleEl.GetString();
                    _logger.LogDebug("TryExtractPythonChartJson: Found title={Title}", payload.ChartTitle);
                }
                
                // Extract insight highlights
                if (root.TryGetProperty("insightHighlights", out var insightsEl) && insightsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var insight in insightsEl.EnumerateArray())
                    {
                        if (insight.ValueKind == JsonValueKind.String)
                        {
                            var insightText = insight.GetString();
                            if (!string.IsNullOrWhiteSpace(insightText))
                            {
                                payload.InsightHighlights.Add(insightText);
                            }
                        }
                    }
                    _logger.LogDebug("TryExtractPythonChartJson: Found {Count} insights", payload.InsightHighlights.Count);
                }
                
                // Extract content (summary text)
                if (root.TryGetProperty("content", out var contentEl) && contentEl.ValueKind == JsonValueKind.String)
                {
                    payload.SummaryText = contentEl.GetString();
                    _logger.LogDebug("TryExtractPythonChartJson: Found content length={Length}", payload.SummaryText?.Length ?? 0);
                }
                
                // Now parse the nested chart object
                root = chartEl;
            }
            else
            {
                _logger.LogDebug("TryExtractPythonChartJson: Old format detected, chart data at root level");
            }

            // Extract chart_type (works for both old and new formats)
            if (root.TryGetProperty("chart_type", out var chartTypeEl) && chartTypeEl.ValueKind == JsonValueKind.String)
            {
                payload.ChartType = chartTypeEl.GetString();
                _logger.LogDebug("TryExtractPythonChartJson: Found chart_type={ChartType}", payload.ChartType);
            }
            else
            {
                _logger.LogWarning("TryExtractPythonChartJson: No chart_type property found or wrong type");
                return false;
            }

            // Build chartData from Python format to ApexCharts format
            var chartData = new Dictionary<string, object?>();

            // Extract labels
            if (root.TryGetProperty("labels", out var labelsEl) && labelsEl.ValueKind == JsonValueKind.Array)
            {
                var labels = ReadStringArray(labelsEl);
                chartData["labels"] = labels;
                _logger.LogDebug("TryExtractPythonChartJson: Found {Count} labels", labels.Length);
            }

            // Extract data/series
            if (root.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.Array)
            {
                var series = ReadNumberArray(dataEl);
                chartData["series"] = series;
                _logger.LogDebug("TryExtractPythonChartJson: Found {Count} data points", series.Length);
            }
            else if (root.TryGetProperty("series", out var seriesEl) && seriesEl.ValueKind == JsonValueKind.Array)
            {
                // Check if series is array of numbers or array of objects
                var first = seriesEl.EnumerateArray().FirstOrDefault();
                if (first.ValueKind == JsonValueKind.Number)
                {
                    var series = ReadNumberArray(seriesEl);
                    chartData["series"] = series;
                    _logger.LogDebug("TryExtractPythonChartJson: Found {Count} series numbers", series.Length);
                }
                else if (first.ValueKind == JsonValueKind.Object)
                {
                    // Already in ApexCharts format
                    chartData["series"] = JsonSerializer.Deserialize<object>(seriesEl.GetRawText());
                    _logger.LogDebug("TryExtractPythonChartJson: Found series objects");
                }
            }

            payload.ChartData = chartData;

            // Try to extract chart title from summary text
            if (string.IsNullOrWhiteSpace(payload.ChartTitle) && !string.IsNullOrWhiteSpace(payload.SummaryText))
            {
                // Use first line as title if it's short
                var lines = payload.SummaryText.Split('\n');
                if (lines.Length > 0 && lines[0].Length < 100)
                {
                    payload.ChartTitle = lines[0].Trim();
                }
            }

            var success = !string.IsNullOrWhiteSpace(payload.ChartType) && payload.ChartData != null;
            _logger.LogInformation("TryExtractPythonChartJson: Success={Success}, ChartType={ChartType}, DataKeys={DataKeys}", 
                success, payload.ChartType, payload.ChartData != null ? string.Join(",", ((Dictionary<string, object?>)payload.ChartData).Keys) : "null");
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TryExtractPythonChartJson: Exception during parsing - {Message}", ex.Message);
            return false;
        }
    }

    private async Task SendAgentMessageAsync(
        ChatMessageDto triggerMessage, 
        string content, 
        MessageType type, 
        CancellationToken cancellationToken,
        Guid? crawlJobId = null,
        string? metadataJson = null)
    {
        var agentMessage = new CrawlerChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = triggerMessage.ConversationId,
            AssignmentId = triggerMessage.AssignmentId ?? Guid.Empty,
            GroupId = triggerMessage.GroupId,
            SenderId = Guid.Empty,
            MessageContent = content,
            MessageType = type,
            CrawlJobId = crawlJobId,
            IsSystemMessage = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty,
            MetadataJson = metadataJson ?? (crawlJobId.HasValue ? JsonSerializer.Serialize(new { latestResultCrawlJobId = crawlJobId }) : null)
        };

        await _unitOfWork.CrawlerChatMessages.AddAsync(agentMessage);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var agentDto = new CrawlerChatMessageDto
        {
            MessageId = agentMessage.Id,
            ConversationId = agentMessage.ConversationId,
            UserId = agentMessage.SenderId,
            UserName = AgentDisplayName,
            Content = agentMessage.MessageContent,
            GroupId = agentMessage.GroupId,
            AssignmentId = agentMessage.AssignmentId == Guid.Empty ? null : agentMessage.AssignmentId,
            MessageType = type,
            CrawlJobId = agentMessage.CrawlJobId,
            Timestamp = agentMessage.CreatedAt,
            VisualizationData = metadataJson // Pass the metadata JSON string directly
        };

        await Clients.Group($"conversation_{agentDto.ConversationId}")
            .SendAsync("UserMessageReceived", agentDto, cancellationToken);

        if (agentDto.GroupId.HasValue)
        {
            await Clients.Group($"group_{agentDto.GroupId}")
                .SendAsync("GroupMessageReceived", agentDto, cancellationToken);
        }
        
        _logger.LogInformation("Agent response posted for conversation {ConversationId}", agentDto.ConversationId);
    }

    private static string BuildAgentSummaryResponse(string summaryText)
    {
        return "Here's the latest crawl summary:\n\n" + summaryText.Trim();
    }

    private string? GetAccessTokenFromContext()
    {
        var httpContext = Context.GetHttpContext();
        if (httpContext == null) return null;

        // 1. Check Authorization Header
        if (httpContext.Request.Headers.TryGetValue("Authorization", out var authHeader) && 
            !string.IsNullOrEmpty(authHeader))
        {
            var headerValue = authHeader.ToString();
            if (headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return headerValue.Substring("Bearer ".Length).Trim();
            }
            return headerValue;
        }

        // 2. Check Query String (Standard for SignalR WebSockets)
        if (httpContext.Request.Query.TryGetValue("access_token", out var queryToken) && 
            !string.IsNullOrEmpty(queryToken))
        {
            return queryToken.ToString();
        }

        return null;
    }
}

/// <summary>
/// DTO for chat messages
/// </summary>
public class ChatMessageDto
{
    public Guid MessageId { get; set; }
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Guid? GroupId { get; set; }
    public Guid? AssignmentId { get; set; }
    public MessageType MessageType { get; set; } = MessageType.UserMessage;
    public Guid? CrawlJobId { get; set; }
    public int? MaxPages { get; set; } // Optional max pages from UI (null = use prompt extraction)
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO for crawler responses
/// </summary>
public class CrawlerResponseDto
{
    public Guid ResponseId { get; set; }
    public Guid ConversationId { get; set; }
    public Guid CrawlJobId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? GroupId { get; set; }
    public Guid? AssignmentId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object>? Metadata { get; set; }
}
