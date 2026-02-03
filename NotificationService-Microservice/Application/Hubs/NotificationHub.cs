using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace NotificationService.Application.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var userRole = Context.User?.FindFirstValue(ClaimTypes.Role);
        
        if (!string.IsNullOrEmpty(userId))
        {
            // Add user to their personal group for targeted notifications
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("User {UserId} connected to NotificationHub with connection {ConnectionId}", 
                userId, Context.ConnectionId);

            // Add staff users to SupportStaff group for support request notifications
            if (userRole == "Staff")
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "SupportStaff");
                _logger.LogInformation("Staff user {UserId} added to SupportStaff group", userId);
            }
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var userRole = Context.User?.FindFirstValue(ClaimTypes.Role);
        
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            
            // Remove staff from SupportStaff group
            if (userRole == "Staff")
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "SupportStaff");
                _logger.LogInformation("Staff user {UserId} removed from SupportStaff group", userId);
            }
            
            _logger.LogInformation("User {UserId} disconnected from NotificationHub", userId);
        }

        if (exception != null)
        {
            _logger.LogError(exception, "User disconnected with error");
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Send notification to a specific user
    /// </summary>
    public async Task SendNotificationToUser(string userId, object notification)
    {
        await Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", notification);
        _logger.LogInformation("Sent notification to user {UserId}", userId);
    }

    /// <summary>
    /// Send notification to multiple users
    /// </summary>
    public async Task SendNotificationToUsers(IEnumerable<string> userIds, object notification)
    {
        var groups = userIds.Select(id => $"user_{id}");
        await Clients.Groups(groups).SendAsync("ReceiveNotification", notification);
        _logger.LogInformation("Sent notification to {Count} users", userIds.Count());
    }

    /// <summary>
    /// Broadcast notification to all connected users
    /// </summary>
    public async Task BroadcastNotification(object notification)
    {
        await Clients.All.SendAsync("ReceiveNotification", notification);
        _logger.LogInformation("Broadcast notification to all users");
    }

    /// <summary>
    /// Client can request their unread count
    /// </summary>
    public async Task RequestUnreadCount()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (!string.IsNullOrEmpty(userId))
        {
            // This will be handled by the controller/service
            await Clients.Caller.SendAsync("UnreadCountRequested", userId);
        }
    }
}
