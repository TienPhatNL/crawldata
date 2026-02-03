using UserService.Domain.Common;

namespace UserService.Domain.Events;

/// <summary>
/// Event raised for security alerts (suspicious login, failed attempts, new device, etc.)
/// </summary>
public class SecurityAlertEvent : BaseEvent
{
    public Guid UserId { get; }
    public string AlertType { get; } // "SuspiciousLogin", "FailedAttempts", "NewDevice", "UnauthorizedAccess"
    public string Details { get; }
    public string? IpAddress { get; }
    public string? Device { get; }
    public string? Location { get; }
    public DateTime OccurredAt { get; }
    public string Severity { get; } // "Low", "Medium", "High", "Critical"

    public SecurityAlertEvent(
        Guid userId,
        string alertType,
        string details,
        DateTime occurredAt,
        string severity = "Medium",
        string? ipAddress = null,
        string? device = null,
        string? location = null)
    {
        UserId = userId;
        AlertType = alertType;
        Details = details;
        OccurredAt = occurredAt;
        Severity = severity;
        IpAddress = ipAddress;
        Device = device;
        Location = location;
    }
}
