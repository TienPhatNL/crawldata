using Confluent.Kafka;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Domain.Common;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Interfaces;
using System.Text.Json;

namespace NotificationService.Infrastructure.Messaging;

public class UserEventConsumer : BaseKafkaConsumer
{
    public UserEventConsumer(
        IOptions<KafkaSettings> kafkaSettings,
        IServiceProvider serviceProvider,
        ILogger<UserEventConsumer> logger)
        : base(kafkaSettings, serviceProvider, logger, "user")
    {
    }

    protected override string[] GetTopics()
    {
        return new[] { "user-events" };
    }

    protected override async Task HandleMessageAsync(ConsumeResult<string, string> consumeResult, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("[UserEventConsumer] Received event from topic {Topic}", consumeResult.Topic);

            var eventType = GetEventType(consumeResult.Message.Headers);
            var eventJson = consumeResult.Message.Value;

            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            // Get the hub context dynamically to avoid circular dependency between Infrastructure and Application
            var hubContextType = Type.GetType("NotificationService.Application.Hubs.NotificationHub, Application");
            var hubContextGenericType = typeof(IHubContext<>).MakeGenericType(hubContextType!);
            var hubContext = (IHubContext)scope.ServiceProvider.GetRequiredService(hubContextGenericType);

            switch (eventType)
            {
                case "UserRegisteredEvent":
                    await HandleUserRegisteredEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "PasswordResetRequestedEvent":
                    await HandlePasswordResetRequestedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "UserProfileUpdatedEvent":
                    await HandleUserProfileUpdatedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "UserRoleChangedEvent":
                    await HandleUserRoleChangedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "SecurityAlertEvent":
                    await HandleSecurityAlertEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "UserApiKeyCreatedEvent":
                    await HandleUserApiKeyCreatedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "UserApiKeyRevokedEvent":
                    await HandleUserApiKeyRevokedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "UserEmailConfirmedEvent":
                    await HandleUserEmailConfirmedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "UserDeletedEvent":
                    await HandleUserDeletedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "UserReactivatedEvent":
                    await HandleUserReactivatedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "UserStatusChangedEvent":
                    await HandleUserStatusChangedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "UserSubscriptionUpgradedEvent":
                    await HandleUserSubscriptionUpgradedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "UserSubscriptionCancelledEvent":
                    await HandleUserSubscriptionCancelledEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "UserSuspendedEvent":
                    await HandleUserSuspendedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "UserCacheInvalidationEvent":
                    // No notification needed - internal system event
                    _logger.LogDebug("[UserEventConsumer] Cache invalidation event received for user, no notification sent");
                    break;
                default:
                    _logger.LogWarning("[UserEventConsumer] Unknown event type: {EventType}", eventType);
                    break;
            }

            _logger.LogInformation("[UserEventConsumer] Processed event: {EventType}", eventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserEventConsumer] Error handling user event");
            throw;
        }
    }

    private string? GetEventType(Headers headers)
    {
        var eventTypeHeader = headers.FirstOrDefault(h => h.Key == "event-type");
        if (eventTypeHeader != null)
        {
            return System.Text.Encoding.UTF8.GetString(eventTypeHeader.GetValueBytes());
        }
        return null;
    }

    private async Task HandleUserRegisteredEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<UserRegisteredEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = @event.UserId,
            Title = "Welcome to CrawlData Platform! 🎉",
            Content = $"Your account has been created successfully. Please verify your email to get started.",
            Type = NotificationType.System,
            Priority = NotificationPriority.Low,
            Source = EventSource.UserService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new { Event = "UserRegistered", Role = @event.Role })
        };

        await unitOfWork.Notifications.AddAsync(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Send via SignalR to user if online
        await hubContext.Clients.User(@event.UserId.ToString())
            .SendAsync("ReceiveNotification", notification, cancellationToken);

        _logger.LogInformation("[UserEventConsumer] Created welcome notification for user {UserId}", @event.UserId);
    }

    private async Task HandlePasswordResetRequestedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<PasswordResetRequestedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = @event.UserId,
            Title = "Password Reset Requested 🔐",
            Content = $"A password reset was requested for your account. If this wasn't you, please secure your account immediately.",
            Type = NotificationType.System,
            Priority = NotificationPriority.High,
            Source = EventSource.UserService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new { Event = "PasswordResetRequested", IpAddress = @event.IpAddress, RequestedAt = @event.RequestedAt })
        };

        await unitOfWork.Notifications.AddAsync(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Send via SignalR
        await hubContext.Clients.User(@event.UserId.ToString())
            .SendAsync("ReceiveNotification", notification, cancellationToken);

        _logger.LogInformation("[UserEventConsumer] Created password reset notification for user {UserId}", @event.UserId);
    }

    private async Task HandleUserProfileUpdatedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<UserProfileUpdatedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = @event.UserId,
            Title = "Profile Updated ✓",
            Content = "Your profile information has been updated successfully.",
            Type = NotificationType.System,
            Priority = NotificationPriority.Low,
            Source = EventSource.UserService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new { Event = "ProfileUpdated", ChangedFields = @event.ChangedFields })
        };

        await unitOfWork.Notifications.AddAsync(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await hubContext.Clients.User(@event.UserId.ToString())
            .SendAsync("ReceiveNotification", notification, cancellationToken);

        _logger.LogInformation("[UserEventConsumer] Created profile update notification for user {UserId}", @event.UserId);
    }

    private async Task HandleUserRoleChangedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<UserRoleChangedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = @event.UserId,
            Title = "Role Changed 🎭",
            Content = $"Your role has been changed from {@event.OldRole} to {@event.NewRole}. Your new permissions are now active.",
            Type = NotificationType.System,
            Priority = NotificationPriority.High,
            Source = EventSource.UserService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new { Event = "RoleChanged", OldRole = @event.OldRole, NewRole = @event.NewRole, ChangedAt = @event.ChangedAt })
        };

        await unitOfWork.Notifications.AddAsync(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await hubContext.Clients.User(@event.UserId.ToString())
            .SendAsync("ReceiveNotification", notification, cancellationToken);

        _logger.LogInformation("[UserEventConsumer] Created role change notification for user {UserId}", @event.UserId);
    }

    private async Task HandleSecurityAlertEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<SecurityAlertEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = @event.UserId,
            Title = "Security Alert! ⚠️",
            Content = @event.Details,
            Type = NotificationType.System,
            Priority = @event.Severity == "Critical" ? NotificationPriority.Urgent : NotificationPriority.High,
            Source = EventSource.UserService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new 
            { 
                Event = "SecurityAlert", 
                AlertType = @event.AlertType, 
                Severity = @event.Severity,
                IpAddress = @event.IpAddress,
                Device = @event.Device,
                Location = @event.Location,
                OccurredAt = @event.OccurredAt
            })
        };

        await unitOfWork.Notifications.AddAsync(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await hubContext.Clients.User(@event.UserId.ToString())
            .SendAsync("ReceiveNotification", notification, cancellationToken);

        _logger.LogInformation("[UserEventConsumer] Created security alert notification for user {UserId}", @event.UserId);
    }

    private async Task HandleUserApiKeyCreatedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<UserApiKeyCreatedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = @event.UserId,
            Title = "API Key Created 🔑",
            Content = $"A new API key '{@event.KeyName}' has been created for your account.",
            Type = NotificationType.System,
            Priority = NotificationPriority.Normal,
            Source = EventSource.UserService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new { Event = "ApiKeyCreated", KeyName = @event.KeyName, ApiKeyId = @event.ApiKeyId })
        };

        await unitOfWork.Notifications.AddAsync(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await hubContext.Clients.Group($"user_{@event.UserId}").SendAsync("ReceiveNotification", notification, cancellationToken);

        _logger.LogInformation("[UserEventConsumer] Created API key created notification for user {UserId}", @event.UserId);
    }

    private async Task HandleUserApiKeyRevokedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<UserApiKeyRevokedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = @event.UserId,
            Title = "API Key Revoked 🔒",
            Content = $"Your API key '{@event.KeyName}' has been revoked and is no longer valid.",
            Type = NotificationType.System,
            Priority = NotificationPriority.High,
            Source = EventSource.UserService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new { Event = "ApiKeyRevoked", KeyName = @event.KeyName, RevokedBy = @event.RevokedBy })
        };

        await unitOfWork.Notifications.AddAsync(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await hubContext.Clients.Group($"user_{@event.UserId}").SendAsync("ReceiveNotification", notification, cancellationToken);

        _logger.LogInformation("[UserEventConsumer] Created API key revoked notification for user {UserId}", @event.UserId);
    }

    private async Task HandleUserEmailConfirmedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<UserEmailConfirmedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = @event.UserId,
            Title = "Email Confirmed ✅",
            Content = $"Your email address {@event.Email} has been successfully verified.",
            Type = NotificationType.System,
            Priority = NotificationPriority.Normal,
            Source = EventSource.UserService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new { Event = "EmailConfirmed", Email = @event.Email })
        };

        await unitOfWork.Notifications.AddAsync(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await hubContext.Clients.Group($"user_{@event.UserId}").SendAsync("ReceiveNotification", notification, cancellationToken);

        _logger.LogInformation("[UserEventConsumer] Created email confirmed notification for user {UserId}", @event.UserId);
    }

    private async Task HandleUserDeletedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<UserDeletedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = @event.UserId,
            Title = "Account Deletion Scheduled 🗑️",
            Content = "Your account has been scheduled for deletion. You have 30 days to cancel this action.",
            Type = NotificationType.System,
            Priority = NotificationPriority.High,
            Source = EventSource.UserService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new { Event = "UserDeleted", DeletedBy = @event.DeletedBy })
        };

        await unitOfWork.Notifications.AddAsync(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await hubContext.Clients.Group($"user_{@event.UserId}").SendAsync("ReceiveNotification", notification, cancellationToken);

        _logger.LogInformation("[UserEventConsumer] Created account deletion notification for user {UserId}", @event.UserId);
    }

    private async Task HandleUserReactivatedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<UserReactivatedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = @event.UserId,
            Title = "Account Reactivated 🎊",
            Content = "Welcome back! Your account has been successfully reactivated.",
            Type = NotificationType.System,
            Priority = NotificationPriority.Normal,
            Source = EventSource.UserService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new { Event = "UserReactivated" })
        };

        await unitOfWork.Notifications.AddAsync(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await hubContext.Clients.Group($"user_{@event.UserId}").SendAsync("ReceiveNotification", notification, cancellationToken);

        _logger.LogInformation("[UserEventConsumer] Created account reactivated notification for user {UserId}", @event.UserId);
    }

    private async Task HandleUserStatusChangedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<UserStatusChangedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        // Only notify on important status changes
        if (@event.NewStatus == "Inactive" || @event.NewStatus == "Suspended")
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = @event.UserId,
                Title = $"Account Status Changed: {@event.NewStatus} ⚠️",
                Content = $"Your account status has been changed to {@event.NewStatus}.",
                Type = NotificationType.System,
                Priority = NotificationPriority.High,
                Source = EventSource.UserService,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                MetadataJson = JsonSerializer.Serialize(new { Event = "UserStatusChanged", OldStatus = @event.OldStatus, NewStatus = @event.NewStatus })
            };

            await unitOfWork.Notifications.AddAsync(notification);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await hubContext.Clients.Group($"user_{@event.UserId}").SendAsync("ReceiveNotification", notification, cancellationToken);

            _logger.LogInformation("[UserEventConsumer] Created status changed notification for user {UserId}", @event.UserId);
        }
    }

    private async Task HandleUserSubscriptionUpgradedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<UserSubscriptionUpgradedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = @event.UserId,
            Title = "Subscription Upgraded! 🎉",
            Content = $"Your subscription has been upgraded from {@event.OldTier} to {@event.NewTier}. Enjoy your new features!",
            Type = NotificationType.SubscriptionExpiring,
            Priority = NotificationPriority.Normal,
            Source = EventSource.UserService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new { Event = "SubscriptionUpgraded", OldTier = @event.OldTier, NewTier = @event.NewTier, NewQuotaLimit = @event.NewQuotaLimit })
        };

        await unitOfWork.Notifications.AddAsync(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await hubContext.Clients.Group($"user_{@event.UserId}").SendAsync("ReceiveNotification", notification, cancellationToken);

        _logger.LogInformation("[UserEventConsumer] Created subscription upgraded notification for user {UserId}", @event.UserId);
    }

    private async Task HandleUserSubscriptionCancelledEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<UserSubscriptionCancelledEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = @event.UserId,
            Title = "Subscription Cancelled 😢",
            Content = $"Your {@event.CancelledTier} subscription has been cancelled. Access will continue until {@event.EndsAt:MMM dd, yyyy}.",
            Type = NotificationType.SubscriptionExpiring,
            Priority = NotificationPriority.High,
            Source = EventSource.UserService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new { Event = "SubscriptionCancelled", CancelledTier = @event.CancelledTier, EndsAt = @event.EndsAt })
        };

        await unitOfWork.Notifications.AddAsync(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await hubContext.Clients.Group($"user_{@event.UserId}").SendAsync("ReceiveNotification", notification, cancellationToken);

        _logger.LogInformation("[UserEventConsumer] Created subscription cancelled notification for user {UserId}", @event.UserId);
    }

    private async Task HandleUserSuspendedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<UserSuspendedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = @event.UserId,
            Title = "Account Suspended 🚫",
            Content = $"Your account has been suspended. Reason: {@event.Reason}. Please contact support if you believe this is an error.",
            Type = NotificationType.System,
            Priority = NotificationPriority.Urgent,
            Source = EventSource.UserService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new { Event = "UserSuspended", Reason = @event.Reason, SuspendedBy = @event.SuspendedBy, SuspendedUntil = @event.SuspendedUntil })
        };

        await unitOfWork.Notifications.AddAsync(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await hubContext.Clients.Group($"user_{@event.UserId}").SendAsync("ReceiveNotification", notification, cancellationToken);

        _logger.LogInformation("[UserEventConsumer] Created account suspended notification for user {UserId}", @event.UserId);
    }
}

// DTOs for deserializing events
public class UserRegisteredEventDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class PasswordResetRequestedEventDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public string? IpAddress { get; set; }
}

public class UserProfileUpdatedEventDto
{
    public Guid UserId { get; set; }
    public Dictionary<string, object> ChangedFields { get; set; } = new();
}

public class UserRoleChangedEventDto
{
    public Guid UserId { get; set; }
    public string OldRole { get; set; } = string.Empty;
    public string NewRole { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
}

public class SecurityAlertEventDto
{
    public Guid UserId { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? Device { get; set; }
    public string? Location { get; set; }
    public DateTime OccurredAt { get; set; }
}

public class UserApiKeyCreatedEventDto
{
    public Guid UserId { get; set; }
    public Guid ApiKeyId { get; set; }
    public string KeyName { get; set; } = string.Empty;
}

public class UserApiKeyRevokedEventDto
{
    public Guid UserId { get; set; }
    public string KeyName { get; set; } = string.Empty;
    public Guid RevokedBy { get; set; }
}

public class UserEmailConfirmedEventDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
}

public class UserDeletedEventDto
{
    public Guid UserId { get; set; }
    public Guid DeletedBy { get; set; }
    public DateTime DeletedAt { get; set; }
}

public class UserReactivatedEventDto
{
    public Guid UserId { get; set; }
    public DateTime ReactivatedAt { get; set; }
}

public class UserStatusChangedEventDto
{
    public Guid UserId { get; set; }
    public string OldStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
}

public class UserSubscriptionUpgradedEventDto
{
    public Guid UserId { get; set; }
    public string OldTier { get; set; } = string.Empty;
    public string NewTier { get; set; } = string.Empty;
    public int NewQuotaLimit { get; set; }
}

public class UserSubscriptionCancelledEventDto
{
    public Guid UserId { get; set; }
    public string CancelledTier { get; set; } = string.Empty;
    public DateTime EndsAt { get; set; }
}

public class UserSuspendedEventDto
{
    public Guid UserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid SuspendedBy { get; set; }
    public DateTime? SuspendedUntil { get; set; }
}


