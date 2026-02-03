namespace ClassroomService.Domain.Messages;

/// <summary>
/// Response message for user validation
/// </summary>
public class UserValidationResponse
{
    public string CorrelationId { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime RespondedAt { get; set; } = DateTime.UtcNow;
}
