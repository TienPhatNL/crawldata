using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace ClassroomService.Application.Hubs;

/// <summary>
/// SignalR Hub for real-time report collaboration
/// Handles WebSocket connections, broadcasts changes, and manages user presence
/// </summary>
[Authorize]
public class ReportCollaborationHub : Hub
{
    private readonly IReportCollaborationBufferService _bufferService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IKafkaUserService _userService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IReportManualSaveService _manualSaveService;
    private readonly ILogger<ReportCollaborationHub> _logger;
    private readonly IHubContext<ReportCollaborationHub> _hubContext;

    // Server-side debouncing (500ms)
    private static readonly ConcurrentDictionary<Guid, Timer> _debounceTimers = new();
    private const int SERVER_DEBOUNCE_MS = 500;

    public ReportCollaborationHub(
        IReportCollaborationBufferService bufferService,
        ICurrentUserService currentUserService,
        IKafkaUserService userService,
        IUnitOfWork unitOfWork,
        IReportManualSaveService manualSaveService,
        ILogger<ReportCollaborationHub> logger,
        IHubContext<ReportCollaborationHub> hubContext)
    {
        _bufferService = bufferService;
        _currentUserService = currentUserService;
        _userService = userService;
        _unitOfWork = unitOfWork;
        _manualSaveService = manualSaveService;
        _logger = logger;
        _hubContext = hubContext;
    }

    /// <summary>
    /// User joins a report collaboration session
    /// </summary>
    public async Task JoinReport(Guid reportId)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
            {
                _logger.LogWarning("User attempted to join report without authentication");
                await Clients.Caller.SendAsync("Error", new { message = "Authentication required" });
                return;
            }

            // Only students can participate in collaboration
            var userRole = _currentUserService.Role;
            if (userRole != "Student")
            {
                _logger.LogWarning("User {UserId} with role {Role} attempted to join collaboration (only students allowed)",
                    userId.Value, userRole);
                await Clients.Caller.SendAsync("Error", new { message = "Only students can participate in report collaboration" });
                return;
            }

            // Fetch the current report to validate status
            var report = await _unitOfWork.Reports.GetByIdAsync(reportId);
            if (report == null)
            {
                _logger.LogWarning("Report {ReportId} not found", reportId);
                await Clients.Caller.SendAsync("Error", new { message = "Report not found" });
                return;
            }

            // Can only join collaboration on Draft or RequiresRevision status reports
            if (report.Status != Domain.Enums.ReportStatus.Draft && 
                report.Status != Domain.Enums.ReportStatus.RequiresRevision)
            {
                _logger.LogWarning("User {UserId} attempted to join report {ReportId} with status {Status} (only Draft or RequiresRevision allowed)", 
                    userId.Value, reportId, report.Status);
                await Clients.Caller.SendAsync("Error", new { message = $"Cannot collaborate on reports with status '{report.Status}'. Only Draft or reports requiring revision allow collaboration." });
                return;
            }

            // Validate assignment type: only group assignments can use collaboration hub
            if (!report.IsGroupSubmission)
            {
                _logger.LogWarning("User {UserId} attempted to join individual assignment {ReportId} via collaboration hub", 
                    userId.Value, reportId);
                await Clients.Caller.SendAsync("Error", new { message = "Individual assignments cannot use the collaboration hub. Please use the update report API instead." });
                return;
            }

            // Validate group membership: user must be a member of the report's group
            if (!report.GroupId.HasValue)
            {
                _logger.LogError("Group report {ReportId} has no GroupId", reportId);
                await Clients.Caller.SendAsync("Error", new { message = "Report has no associated group" });
                return;
            }

            var group = await _unitOfWork.Groups.GetGroupWithMembersAsync(report.GroupId.Value);
            if (group == null)
            {
                _logger.LogError("Group {GroupId} not found for report {ReportId}", report.GroupId.Value, reportId);
                await Clients.Caller.SendAsync("Error", new { message = "Group not found" });
                return;
            }

            bool isMember = false;
            foreach (var member in group.Members)
            {
                var enrollment = await _unitOfWork.CourseEnrollments.GetAsync(e => e.Id == member.EnrollmentId);
                if (enrollment != null && enrollment.StudentId == userId.Value)
                {
                    isMember = true;
                    break;
                }
            }

            if (!isMember)
            {
                _logger.LogWarning("User {UserId} attempted to join report {ReportId} but is not a member of group {GroupId}", 
                    userId.Value, reportId, report.GroupId.Value);
                await Clients.Caller.SendAsync("Error", new { message = "You must be a member of this group to collaborate on this report" });
                return;
            }

            _logger.LogInformation("✅ User {UserId} verified as member of group {GroupId} for report {ReportId}", 
                userId.Value, report.GroupId.Value, reportId);

            // Get user information
            var userInfo = await _userService.GetUserByIdAsync(userId.Value);
            if (userInfo == null)
            {
                _logger.LogWarning("User {UserId} not found", userId.Value);
                await Clients.Caller.SendAsync("Error", new { message = "User information not found" });
                return;
            }

            // Add to SignalR group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"report:{reportId}");

            // FIX: Remove existing user first to prevent duplicates
            await _bufferService.RemoveUserFromSessionAsync(reportId, userId.Value);
            
            // Add to buffer service session (fresh entry)
            await _bufferService.AddUserToSessionAsync(
                reportId, 
                userId.Value, 
                userInfo.FullName, 
                userInfo.Email);

            // Get current active users
            var activeUsers = await _bufferService.GetActiveUsersAsync(reportId);

            // Get the latest working content (from buffer if available, otherwise from database)
            var latestContent = await _bufferService.GetLatestWorkingContentAsync(reportId);
            var currentContent = latestContent ?? report.Submission;

            _logger.LogInformation("👤 User {UserId} joining: sending content (source: {Source}, length: {Length})", 
                userId.Value, latestContent != null ? "Redis buffer" : "Database", currentContent?.Length ?? 0);

            // Notify the joining user about current session state WITH current content
            await Clients.Caller.SendAsync("SessionJoined", new
            {
                reportId,
                activeUsers,
                currentContent = currentContent,
                version = report.Version,
                message = "Successfully joined collaboration session"
            });

            // Notify other users in the report
            await Clients.OthersInGroup($"report:{reportId}").SendAsync("UserJoined", new
            {
                userId = userId.Value,
                userName = userInfo.FullName,
                userEmail = userInfo.Email,
                timestamp = DateTime.UtcNow
            });

            _logger.LogInformation("User {UserId} joined report {ReportId} collaboration", 
                userId.Value, reportId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining report {ReportId}", reportId);
            await Clients.Caller.SendAsync("Error", new { message = "Failed to join collaboration session" });
        }
    }

    /// <summary>
    /// User leaves a report collaboration session
    /// </summary>
    public async Task LeaveReport(Guid reportId)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue) return;

            // Remove from SignalR group
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"report:{reportId}");

            // Remove from buffer service session
            await _bufferService.RemoveUserFromSessionAsync(reportId, userId.Value);

            // Check if this was the last user - if so, save immediately
            var remainingUsers = await _bufferService.GetActiveUsersAsync(reportId);
            if (remainingUsers == null || !remainingUsers.Any())
            {
                _logger.LogInformation("🚨 Last user left report {ReportId} - triggering immediate save", reportId);
                
                // Check if there are pending changes to save
                var pendingCount = await _bufferService.GetPendingChangesCountAsync(reportId);
                if (pendingCount > 0)
                {
                    try
                    {
                        // Trigger immediate save
                        var saveResult = await _manualSaveService.ForceSaveAsync(reportId, userId.Value);
                        
                        if (saveResult.Success)
                        {
                            _logger.LogInformation("✅ Auto-saved version {Version} after last user left report {ReportId}", 
                                saveResult.NewVersion, reportId);
                        }
                        else
                        {
                            _logger.LogWarning("⚠️ Auto-save failed after last user left report {ReportId}: {Message}", 
                                reportId, saveResult.Message);
                        }
                    }
                    catch (Exception saveEx)
                    {
                        _logger.LogError(saveEx, "❌ Error auto-saving after last user left report {ReportId}", reportId);
                    }
                }
                else
                {
                    _logger.LogInformation("💤 No pending changes to save for report {ReportId}", reportId);
                }
            }

            // Notify other users
            await Clients.OthersInGroup($"report:{reportId}").SendAsync("UserLeft", new
            {
                userId = userId.Value,
                timestamp = DateTime.UtcNow
            });

            _logger.LogInformation("User {UserId} left report {ReportId} collaboration", 
                userId.Value, reportId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving report {ReportId}", reportId);
        }
    }

    /// <summary>
    /// Broadcast a content change to all collaborators
    /// Server-side debouncing: stores change in pending buffer and broadcasts after 500ms of silence
    /// </summary>
    public async Task BroadcastChange(ReportChangeDto change)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue) return;

            // Business Rule: Only students can edit
            var userRole = _currentUserService.Role;
            if (userRole != "Student")
            {
                _logger.LogWarning("User {UserId} with role {Role} attempted to broadcast change (only students allowed)", 
                    userId.Value, userRole);
                await Clients.Caller.SendAsync("Error", new { message = "Only students can edit reports" });
                return;
            }

            // Validate report exists and status
            var report = await _unitOfWork.Reports.GetByIdAsync(change.ReportId);
            if (report == null)
            {
                _logger.LogWarning("Report {ReportId} not found", change.ReportId);
                await Clients.Caller.SendAsync("Error", new { message = "Report not found" });
                return;
            }
            // Can only edit Draft or RequiresRevision status reports
            if (report.Status != Domain.Enums.ReportStatus.Draft && 
                report.Status != Domain.Enums.ReportStatus.RequiresRevision)
            {
                _logger.LogWarning("User {UserId} attempted to edit report {ReportId} with status {Status}", 
                    userId.Value, change.ReportId, report.Status);
                await Clients.Caller.SendAsync("Error", new { message = "Can only edit Draft or reports requiring revision" });
                return;
            }

            // Validate assignment type: only group assignments can use collaboration hub
            if (!report.IsGroupSubmission)
            {
                _logger.LogWarning("User {UserId} attempted to edit individual assignment {ReportId} via collaboration hub", 
                    userId.Value, change.ReportId);
                await Clients.Caller.SendAsync("Error", new { message = "Individual assignments cannot use the collaboration hub" });
                return;
            }

            // Validate group membership
            if (!report.GroupId.HasValue)
            {
                _logger.LogError("Group report {ReportId} has no GroupId", change.ReportId);
                await Clients.Caller.SendAsync("Error", new { message = "Report has no associated group" });
                return;
            }

            var group = await _unitOfWork.Groups.GetGroupWithMembersAsync(report.GroupId.Value);
            if (group == null)
            {
                _logger.LogError("Group {GroupId} not found for report {ReportId}", report.GroupId.Value, change.ReportId);
                await Clients.Caller.SendAsync("Error", new { message = "Group not found" });
                return;
            }

            bool isMember = false;
            foreach (var member in group.Members)
            {
                var enrollment = await _unitOfWork.CourseEnrollments.GetAsync(e => e.Id == member.EnrollmentId);
                if (enrollment != null && enrollment.StudentId == userId.Value)
                {
                    isMember = true;
                    break;
                }
            }

            if (!isMember)
            {
                _logger.LogWarning("User {UserId} attempted to edit report {ReportId} but is not a member of group {GroupId}", 
                    userId.Value, change.ReportId, report.GroupId.Value);
                await Clients.Caller.SendAsync("Error", new { message = "You must be a member of this group to edit this report" });
                return;
            }

            // Ensure the change is from the current user
            change.UserId = userId.Value;
            change.Timestamp = DateTime.UtcNow;

            // Store pending change in Redis (replaces previous pending change)
            await _bufferService.SetPendingChangeAsync(change.ReportId, change);

            // Cancel existing timer if any
            if (_debounceTimers.TryRemove(change.ReportId, out var existingTimer))
            {
                existingTimer.Dispose();
            }

            // Create new timer that will broadcast after 500ms of silence
            var timer = new Timer(async _ =>
            {
                try
                {
                    // Get the pending change
                    var pendingChange = await _bufferService.GetPendingChangeAsync(change.ReportId);
                    if (pendingChange != null)
                    {
                        // Store in buffer for version creation later
                        await _bufferService.AddChangeAsync(pendingChange);

                        // Broadcast to all OTHER users using HubContext (Hub instance may be disposed)
                        await _hubContext.Clients.Group($"report:{pendingChange.ReportId}").SendAsync("ReceiveChange", pendingChange);

                        // Clear pending change
                        await _bufferService.ClearPendingChangeAsync(change.ReportId);

                        _logger.LogDebug("⏱️ Debounced broadcast from user {UserId} in report {ReportId} (after 500ms silence)", 
                            pendingChange.UserId, pendingChange.ReportId);
                    }

                    // Remove timer from dictionary
                    if (_debounceTimers.TryRemove(change.ReportId, out Timer? disposedTimer))
                    {
                        disposedTimer?.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in debounce timer for report {ReportId}", change.ReportId);
                }
            }, null, SERVER_DEBOUNCE_MS, Timeout.Infinite);

            // Store new timer
            _debounceTimers[change.ReportId] = timer;

            _logger.LogTrace("📝 Pending change from user {UserId} in report {ReportId} (debouncing 500ms)", 
                userId.Value, change.ReportId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting change for report {ReportId}", change.ReportId);
            await Clients.Caller.SendAsync("Error", new { message = "Failed to broadcast change" });
        }
    }

    /// <summary>
    /// Get current active collaborators
    /// </summary>
    public async Task<List<CollaboratorPresenceDto>> GetActiveCollaborators(Guid reportId)
    {
        try
        {
            return await _bufferService.GetActiveUsersAsync(reportId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active collaborators for report {ReportId}", reportId);
            return new List<CollaboratorPresenceDto>();
        }
    }

    /// <summary>
    /// Handle connection disconnect (unexpected disconnects like browser close, network issue)
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (userId.HasValue)
            {
                _logger.LogInformation("User {UserId} disconnected from SignalR (unexpected disconnect)", userId.Value);
                
                // Clean up user from all active report sessions they might be in
                var activeSessions = await _bufferService.GetAllActiveSessionsAsync();
                
                foreach (var reportId in activeSessions)
                {
                    try
                    {
                        // Check if user was in this session
                        var activeUsers = await _bufferService.GetActiveUsersAsync(reportId);
                        var userWasInSession = activeUsers?.Any(u => u.UserId == userId.Value) ?? false;
                        
                        if (userWasInSession)
                        {
                            _logger.LogInformation("Cleaning up user {UserId} from report {ReportId} session after disconnect", 
                                userId.Value, reportId);
                            
                            // Remove user from session
                            await _bufferService.RemoveUserFromSessionAsync(reportId, userId.Value);
                            
                            // Check if this was the last user - if so, save immediately
                            var remainingUsers = await _bufferService.GetActiveUsersAsync(reportId);
                            if (remainingUsers == null || !remainingUsers.Any())
                            {
                                _logger.LogInformation("🚨 Last user disconnected from report {ReportId} - triggering immediate save", reportId);
                                
                                var pendingCount = await _bufferService.GetPendingChangesCountAsync(reportId);
                                if (pendingCount > 0)
                                {
                                    try
                                    {
                                        var saveResult = await _manualSaveService.ForceSaveAsync(reportId, userId.Value);
                                        
                                        if (saveResult.Success)
                                        {
                                            _logger.LogInformation("✅ Auto-saved version {Version} after last user disconnected from report {ReportId}", 
                                                saveResult.NewVersion, reportId);
                                        }
                                    }
                                    catch (Exception saveEx)
                                    {
                                        _logger.LogError(saveEx, "❌ Error auto-saving after disconnect for report {ReportId}", reportId);
                                    }
                                }
                            }
                            
                            // Notify remaining users
                            await Clients.Group($"report:{reportId}").SendAsync("UserLeft", new
                            {
                                userId = userId.Value,
                                timestamp = DateTime.UtcNow,
                                reason = "disconnected"
                            });
                        }
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogError(cleanupEx, "Error cleaning up user {UserId} from report {ReportId}", 
                            userId.Value, reportId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling disconnection");
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Handle connection established
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (userId.HasValue)
            {
                _logger.LogInformation("User {UserId} connected to SignalR with connection {ConnectionId}", 
                    userId.Value, Context.ConnectionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling connection");
        }

        await base.OnConnectedAsync();
    }
}
