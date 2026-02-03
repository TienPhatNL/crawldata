using ClassroomService.Domain.DTOs;

namespace ClassroomService.Domain.Messages;

/// <summary>
/// Response message with user data from UserService
/// </summary>
public class UserQueryResponse
{
    public string CorrelationId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public List<UserDto> Users { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public DateTime RespondedAt { get; set; } = DateTime.UtcNow;
}
