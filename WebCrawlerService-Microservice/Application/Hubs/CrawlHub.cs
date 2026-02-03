using Microsoft.AspNetCore.SignalR;
using WebCrawlerService.Application.Services;

namespace WebCrawlerService.Application.Hubs;

/// <summary>
/// SignalR hub for real-time crawl job monitoring and updates
/// </summary>
public class CrawlHub : Hub
{
    private readonly ILogger<CrawlHub> _logger;
    private readonly ICrawlerMonitoringService _monitoringService;

    public CrawlHub(
        ILogger<CrawlHub> logger,
        ICrawlerMonitoringService monitoringService)
    {
        _logger = logger;
        _monitoringService = monitoringService;
    }

    /// <summary>
    /// Subscribe to real-time updates for a specific job
    /// </summary>
    /// <param name="jobId">Job ID to monitor</param>
    public async Task SubscribeToJob(string jobId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"job_{jobId}");
        _logger.LogInformation("Client {ConnectionId} subscribed to job {JobId}",
            Context.ConnectionId, jobId);

        // Send initial job stats immediately
        if (Guid.TryParse(jobId, out var guid))
        {
            try
            {
                var stats = await _monitoringService.GetJobStatsAsync(guid);
                if (stats != null)
                {
                    await Clients.Caller.SendAsync("OnJobStats", stats);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send initial stats for job {JobId}", jobId);
            }
        }
    }

    /// <summary>
    /// Unsubscribe from job updates
    /// </summary>
    /// <param name="jobId">Job ID to stop monitoring</param>
    public async Task UnsubscribeFromJob(string jobId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"job_{jobId}");
        _logger.LogInformation("Client {ConnectionId} unsubscribed from job {JobId}",
            Context.ConnectionId, jobId);
    }

    /// <summary>
    /// Subscribe to system-wide metrics (admin/staff only)
    /// </summary>
    public async Task SubscribeToSystemMetrics()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "system_metrics");
        _logger.LogInformation("Client {ConnectionId} subscribed to system metrics",
            Context.ConnectionId);

        // Send initial metrics immediately
        try
        {
            var metrics = await _monitoringService.GetSystemMetricsAsync();
            await Clients.Caller.SendAsync("OnSystemMetrics", metrics);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send initial system metrics");
        }
    }

    /// <summary>
    /// Unsubscribe from system metrics
    /// </summary>
    public async Task UnsubscribeFromSystemMetrics()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "system_metrics");
        _logger.LogInformation("Client {ConnectionId} unsubscribed from system metrics",
            Context.ConnectionId);
    }

    /// <summary>
    /// Subscribe to all jobs (admin/staff dashboard)
    /// </summary>
    public async Task SubscribeToAllJobs()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "all_jobs");
        _logger.LogInformation("Client {ConnectionId} subscribed to all jobs",
            Context.ConnectionId);
    }

    /// <summary>
    /// Unsubscribe from all jobs
    /// </summary>
    public async Task UnsubscribeFromAllJobs()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "all_jobs");
        _logger.LogInformation("Client {ConnectionId} unsubscribed from all jobs",
            Context.ConnectionId);
    }

    /// <summary>
    /// Get current connection ID (useful for debugging)
    /// </summary>
    public string GetConnectionId()
    {
        return Context.ConnectionId;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client {ConnectionId} connected to CrawlHub", Context.ConnectionId);
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

    // ========== Chart Broadcasting Methods ==========

    /// <summary>
    /// Subscribe to real-time chart updates for a specific job
    /// </summary>
    /// <param name="jobId">Job ID to monitor charts for</param>
    public async Task SubscribeToJobCharts(string jobId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"job_charts_{jobId}");
        _logger.LogInformation("Client {ConnectionId} subscribed to charts for job {JobId}",
            Context.ConnectionId, jobId);
    }

    /// <summary>
    /// Unsubscribe from job chart updates
    /// </summary>
    /// <param name="jobId">Job ID to stop monitoring</param>
    public async Task UnsubscribeFromJobCharts(string jobId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"job_charts_{jobId}");
        _logger.LogInformation("Client {ConnectionId} unsubscribed from charts for job {JobId}",
            Context.ConnectionId, jobId);
    }

    /// <summary>
    /// Subscribe to system-wide chart updates (admin/staff only)
    /// </summary>
    public async Task SubscribeToSystemCharts()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "system_charts");
        _logger.LogInformation("Client {ConnectionId} subscribed to system charts",
            Context.ConnectionId);
    }

    /// <summary>
    /// Unsubscribe from system chart updates
    /// </summary>
    public async Task UnsubscribeFromSystemCharts()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "system_charts");
        _logger.LogInformation("Client {ConnectionId} unsubscribed from system charts",
            Context.ConnectionId);
    }

    // ========== Group Collaboration Methods ==========

    /// <summary>
    /// Subscribe to crawl jobs for a specific group
    /// </summary>
    /// <param name="groupId">Group ID to monitor</param>
    public async Task SubscribeToGroupJobs(string groupId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"group_{groupId}");
        _logger.LogInformation("Client {ConnectionId} subscribed to group {GroupId} jobs",
            Context.ConnectionId, groupId);
    }

    /// <summary>
    /// Unsubscribe from group job updates
    /// </summary>
    /// <param name="groupId">Group ID to stop monitoring</param>
    public async Task UnsubscribeFromGroupJobs(string groupId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"group_{groupId}");
        _logger.LogInformation("Client {ConnectionId} unsubscribed from group {GroupId} jobs",
            Context.ConnectionId, groupId);
    }

    /// <summary>
    /// Subscribe to crawl jobs for a specific assignment
    /// </summary>
    /// <param name="assignmentId">Assignment ID to monitor</param>
    public async Task SubscribeToAssignmentJobs(string assignmentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"assignment_{assignmentId}");
        _logger.LogInformation("Client {ConnectionId} subscribed to assignment {AssignmentId} jobs",
            Context.ConnectionId, assignmentId);
    }

    /// <summary>
    /// Unsubscribe from assignment job updates
    /// </summary>
    /// <param name="assignmentId">Assignment ID to stop monitoring</param>
    public async Task UnsubscribeFromAssignmentJobs(string assignmentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"assignment_{assignmentId}");
        _logger.LogInformation("Client {ConnectionId} unsubscribed from assignment {AssignmentId} jobs",
            Context.ConnectionId, assignmentId);
    }

    /// <summary>
    /// Subscribe to a conversation thread's crawl activities
    /// </summary>
    /// <param name="conversationId">Conversation thread ID</param>
    public async Task SubscribeToConversation(string conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
        _logger.LogInformation("Client {ConnectionId} subscribed to conversation {ConversationId}",
            Context.ConnectionId, conversationId);
    }

    /// <summary>
    /// Unsubscribe from conversation crawl updates
    /// </summary>
    /// <param name="conversationId">Conversation thread ID</param>
    public async Task UnsubscribeFromConversation(string conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
        _logger.LogInformation("Client {ConnectionId} unsubscribed from conversation {ConversationId}",
            Context.ConnectionId);
    }

    /// <summary>
    /// Broadcast job update to group members
    /// Used by event handlers to notify all group members of job progress
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <param name="jobUpdate">Job update data</param>
    public async Task BroadcastToGroup(string groupId, object jobUpdate)
    {
        await Clients.Group($"group_{groupId}").SendAsync("OnGroupJobUpdate", jobUpdate);
        _logger.LogInformation("Broadcasted job update to group {GroupId}", groupId);
    }

    /// <summary>
    /// Broadcast job update to assignment participants
    /// </summary>
    /// <param name="assignmentId">Assignment ID</param>
    /// <param name="jobUpdate">Job update data</param>
    public async Task BroadcastToAssignment(string assignmentId, object jobUpdate)
    {
        await Clients.Group($"assignment_{assignmentId}").SendAsync("OnAssignmentJobUpdate", jobUpdate);
        _logger.LogInformation("Broadcasted job update to assignment {AssignmentId}", assignmentId);
    }

    /// <summary>
    /// Broadcast job update to conversation participants
    /// </summary>
    /// <param name="conversationId">Conversation thread ID</param>
    /// <param name="jobUpdate">Job update data</param>
    public async Task BroadcastToConversation(string conversationId, object jobUpdate)
    {
        await Clients.Group($"conversation_{conversationId}").SendAsync("OnConversationJobUpdate", jobUpdate);
        _logger.LogInformation("Broadcasted job update to conversation {ConversationId}", conversationId);
    }
}
