using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace ClassroomService.Infrastructure.Services;

/// <summary>
/// Redis-based buffer service for report collaboration
/// Temporarily stores changes before they are persisted to database
/// </summary>
public class ReportCollaborationBufferService : IReportCollaborationBufferService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<ReportCollaborationBufferService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    
    // Redis key prefixes
    private const string CHANGES_KEY_PREFIX = "report:buffer";
    private const string USERS_KEY_PREFIX = "report:users";
    private const string ACTIVITY_KEY_PREFIX = "report:activity";
    private const string CURSOR_KEY_PREFIX = "report:cursor";
    private const string TYPING_KEY_PREFIX = "report:typing";
    private const string CONTRIBUTORS_KEY_PREFIX = "report:contributors";
    private const string REPORTS_SET_KEY = "report:active";

    public ReportCollaborationBufferService(
        IConnectionMultiplexer redis,
        ILogger<ReportCollaborationBufferService> logger)
    {
        _redis = redis;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task AddChangeAsync(ReportChangeDto change, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = $"{CHANGES_KEY_PREFIX}:{change.ReportId}:changes";
            
            // Serialize and store change
            var json = JsonSerializer.Serialize(change, _jsonOptions);
            await db.ListRightPushAsync(key, json);
            
            // Update last activity timestamp
            await UpdateLastActivityAsync(change.ReportId, cancellationToken);
            
            // Add to contributors set
            await db.SetAddAsync($"{CONTRIBUTORS_KEY_PREFIX}:{change.ReportId}", change.UserId.ToString());
            
            // Add report to active reports set
            await db.SetAddAsync(REPORTS_SET_KEY, change.ReportId.ToString());
            
            _logger.LogInformation("✅ Added change to buffer for report {ReportId} by user {UserId} - Content length: {Length}", 
                change.ReportId, change.UserId, change.Content?.Length ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error adding change to buffer for report {ReportId}", change.ReportId);
            throw;
        }
    }

    public async Task<List<ReportChangeDto>> GetBufferedChangesAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = $"{CHANGES_KEY_PREFIX}:{reportId}:changes";
            
            var changes = await db.ListRangeAsync(key);
            var result = new List<ReportChangeDto>();
            
            foreach (var change in changes)
            {
                if (!change.IsNullOrEmpty)
                {
                    var dto = JsonSerializer.Deserialize<ReportChangeDto>(change.ToString(), _jsonOptions);
                    if (dto != null)
                    {
                        result.Add(dto);
                    }
                }
            }
            
            _logger.LogDebug("Retrieved {Count} buffered changes for report {ReportId}", result.Count, reportId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting buffered changes for report {ReportId}", reportId);
            return new List<ReportChangeDto>();
        }
    }

    public async Task<int> GetPendingChangesCountAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = $"{CHANGES_KEY_PREFIX}:{reportId}:changes";
            return (int)await db.ListLengthAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending changes count for report {ReportId}", reportId);
            return 0;
        }
    }

    public async Task ClearBufferAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            
            // Delete all keys related to this report
            await db.KeyDeleteAsync(new RedisKey[]
            {
                $"{CHANGES_KEY_PREFIX}:{reportId}:changes",
                $"{ACTIVITY_KEY_PREFIX}:{reportId}",
                $"{CONTRIBUTORS_KEY_PREFIX}:{reportId}"
            });
            
            // Remove from active reports set
            await db.SetRemoveAsync(REPORTS_SET_KEY, reportId.ToString());
            
            _logger.LogInformation("Cleared buffer for report {ReportId}", reportId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing buffer for report {ReportId}", reportId);
            throw;
        }
    }

    public async Task AddUserToSessionAsync(Guid reportId, Guid userId, string userName, string userEmail, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = $"{USERS_KEY_PREFIX}:{reportId}";
            
            var user = new CollaboratorPresenceDto
            {
                UserId = userId,
                UserName = userName,
                UserEmail = userEmail,
                LastActivity = DateTime.UtcNow,
                IsTyping = false,
                CursorPosition = 0
            };
            
            var json = JsonSerializer.Serialize(user, _jsonOptions);
            await db.HashSetAsync(key, userId.ToString(), json);
            
            // Set expiration on user session (30 minutes)
            await db.KeyExpireAsync(key, TimeSpan.FromMinutes(30));
            
            _logger.LogDebug("Added user {UserId} to session for report {ReportId}", userId, reportId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user to session for report {ReportId}", reportId);
        }
    }

    public async Task RemoveUserFromSessionAsync(Guid reportId, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = $"{USERS_KEY_PREFIX}:{reportId}";
            await db.HashDeleteAsync(key, userId.ToString());
            
            _logger.LogDebug("Removed user {UserId} from session for report {ReportId}", userId, reportId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user from session for report {ReportId}", reportId);
        }
    }

    public async Task<List<CollaboratorPresenceDto>> GetActiveUsersAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = $"{USERS_KEY_PREFIX}:{reportId}";
            
            var entries = await db.HashGetAllAsync(key);
            var users = new List<CollaboratorPresenceDto>();
            
            foreach (var entry in entries)
            {
                if (!entry.Value.IsNullOrEmpty)
                {
                    var user = JsonSerializer.Deserialize<CollaboratorPresenceDto>(entry.Value.ToString(), _jsonOptions);
                    if (user != null)
                    {
                        users.Add(user);
                    }
                }
            }
            
            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active users for report {ReportId}", reportId);
            return new List<CollaboratorPresenceDto>();
        }
    }

    public async Task<List<Guid>> GetContributorsAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = $"{CONTRIBUTORS_KEY_PREFIX}:{reportId}";
            
            var contributors = await db.SetMembersAsync(key);
            return contributors.Select(c => Guid.Parse(c.ToString())).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contributors for report {ReportId}", reportId);
            return new List<Guid>();
        }
    }

    public async Task UpdateLastActivityAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = $"{ACTIVITY_KEY_PREFIX}:{reportId}";
            await db.StringSetAsync(key, DateTime.UtcNow.ToString("O"));
            
            // Set expiration (1 hour)
            await db.KeyExpireAsync(key, TimeSpan.FromHours(1));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last activity for report {ReportId}", reportId);
        }
    }

    public async Task<TimeSpan> GetInactivityDurationAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        try
        {
            var lastActivity = await GetLastActivityAsync(reportId, cancellationToken);
            
            if (lastActivity == null)
            {
                return TimeSpan.MaxValue;
            }
            
            return DateTime.UtcNow - lastActivity.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inactivity duration for report {ReportId}", reportId);
            return TimeSpan.MaxValue;
        }
    }

    public async Task<DateTime?> GetLastActivityAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = $"{ACTIVITY_KEY_PREFIX}:{reportId}";
            var value = await db.StringGetAsync(key);
            
            if (value.IsNullOrEmpty)
            {
                return null;
            }
            
            if (DateTime.TryParse(value.ToString(), out var lastActivity))
            {
                return lastActivity;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting last activity for report {ReportId}", reportId);
            return null;
        }
    }

    public async Task<List<Guid>> GetReportsWithPendingChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var reportIds = await db.SetMembersAsync(REPORTS_SET_KEY);
            
            return reportIds.Select(id => Guid.Parse(id.ToString())).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reports with pending changes");
            return new List<Guid>();
        }
    }

    public async Task<List<Guid>> GetAllActiveSessionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            // Get all report IDs that have active sessions (from the set we maintain)
            var reportIds = await db.SetMembersAsync(REPORTS_SET_KEY);
            
            var activeSessions = new List<Guid>();
            
            // Check each report to see if it has activity
            foreach (var reportIdValue in reportIds)
            {
                if (Guid.TryParse(reportIdValue.ToString(), out var reportId))
                {
                    var lastActivity = await GetLastActivityAsync(reportId, cancellationToken);
                    if (lastActivity.HasValue)
                    {
                        activeSessions.Add(reportId);
                    }
                }
            }
            
            return activeSessions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all active sessions");
            return new List<Guid>();
        }
    }

    public async Task<bool> ShouldFlushBufferAsync(Guid reportId, int debounceSeconds = 60, int maxBufferSize = 200, 
        int maxBufferMinutes = 5, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check inactivity duration
            var inactivity = await GetInactivityDurationAsync(reportId, cancellationToken);
            if (inactivity.TotalSeconds >= debounceSeconds)
            {
                _logger.LogDebug("Report {ReportId} should flush due to inactivity ({Seconds}s)", 
                    reportId, inactivity.TotalSeconds);
                return true;
            }
            
            // Check buffer size
            var count = await GetPendingChangesCountAsync(reportId, cancellationToken);
            if (count >= maxBufferSize)
            {
                _logger.LogDebug("Report {ReportId} should flush due to buffer size ({Count})", 
                    reportId, count);
                return true;
            }
            
            // Check total buffer time (first change timestamp vs now)
            var changes = await GetBufferedChangesAsync(reportId, cancellationToken);
            if (changes.Any())
            {
                var firstChange = changes.First();
                var elapsed = DateTime.UtcNow - firstChange.Timestamp;
                if (elapsed.TotalMinutes >= maxBufferMinutes)
                {
                    _logger.LogDebug("Report {ReportId} should flush due to max buffer time ({Minutes}m)", 
                        reportId, elapsed.TotalMinutes);
                    return true;
                }
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if buffer should flush for report {ReportId}", reportId);
            return false;
        }
    }

    public async Task<ReportCollaborationSessionDto?> GetSessionInfoAsync(Guid reportId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var activeUsers = await GetActiveUsersAsync(reportId, cancellationToken);
            var pendingChanges = await GetPendingChangesCountAsync(reportId, cancellationToken);
            var lastActivity = await GetLastActivityAsync(reportId, cancellationToken);
            
            return new ReportCollaborationSessionDto
            {
                ReportId = reportId,
                ActiveUsers = activeUsers,
                PendingChangesCount = pendingChanges,
                LastChangeAt = lastActivity,
                HasUnsavedChanges = pendingChanges > 0,
                SessionStartedAt = lastActivity ?? DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session info for report {ReportId}", reportId);
            return null;
        }
    }

    public async Task<string?> GetLatestWorkingContentAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to get the latest content from buffered changes
            var changes = await GetBufferedChangesAsync(reportId, cancellationToken);
            
            if (changes != null && changes.Any())
            {
                // Return the content from the most recent change (last in list)
                var latestChange = changes.Last();
                _logger.LogDebug("📋 Returning latest buffered content for report {ReportId} (length: {Length})", 
                    reportId, latestChange.Content?.Length ?? 0);
                return latestChange.Content;
            }
            
            _logger.LogDebug("📋 No buffered content for report {ReportId}, will use database content", reportId);
            return null; // No buffered content, caller should use database
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest working content for report {ReportId}", reportId);
            return null;
        }
    }

    /// <summary>
    /// Store a pending change that hasn't been broadcasted yet (for server-side debouncing)
    /// </summary>
    public async Task SetPendingChangeAsync(Guid reportId, ReportChangeDto change, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = $"report:pending:{reportId}";
            var json = JsonSerializer.Serialize(change, _jsonOptions);
            await db.StringSetAsync(key, json, TimeSpan.FromSeconds(10)); // Expire in 10s as safety
            _logger.LogTrace("Stored pending change for report {ReportId}", reportId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing pending change for report {ReportId}", reportId);
        }
    }

    /// <summary>
    /// Get the pending change for a report (for server-side debouncing)
    /// </summary>
    public async Task<ReportChangeDto?> GetPendingChangeAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = $"report:pending:{reportId}";
            var json = await db.StringGetAsync(key);
            
            if (json.IsNullOrEmpty)
            {
                return null;
            }
            
            return JsonSerializer.Deserialize<ReportChangeDto>(json.ToString(), _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending change for report {ReportId}", reportId);
            return null;
        }
    }

    /// <summary>
    /// Clear the pending change for a report (for server-side debouncing)
    /// </summary>
    public async Task ClearPendingChangeAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = $"report:pending:{reportId}";
            await db.KeyDeleteAsync(key);
            _logger.LogTrace("Cleared pending change for report {ReportId}", reportId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing pending change for report {ReportId}", reportId);
        }
    }
}
