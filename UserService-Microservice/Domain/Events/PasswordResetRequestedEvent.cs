using UserService.Domain.Common;

namespace UserService.Domain.Events;

/// <summary>
/// Event raised when a user requests a password reset
/// </summary>
public class PasswordResetRequestedEvent : BaseEvent
{
    public Guid UserId { get; }
    public string Email { get; }
    public string ResetToken { get; }
    public DateTime RequestedAt { get; }
    public string? IpAddress { get; }
    public string? Location { get; }

    public PasswordResetRequestedEvent(
        Guid userId, 
        string email, 
        string resetToken, 
        DateTime requestedAt,
        string? ipAddress = null,
        string? location = null)
    {
        UserId = userId;
        Email = email;
        ResetToken = resetToken;
        RequestedAt = requestedAt;
        IpAddress = ipAddress;
        Location = location;
    }
}
