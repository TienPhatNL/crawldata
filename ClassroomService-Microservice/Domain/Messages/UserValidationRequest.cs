namespace ClassroomService.Domain.Messages;

/// <summary>
/// Request message for validating users from UserService
/// </summary>
public class UserValidationRequest
{
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    public Guid UserId { get; set; }
    public string? RequiredRole { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}
