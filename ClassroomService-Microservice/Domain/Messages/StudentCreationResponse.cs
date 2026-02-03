using ClassroomService.Domain.DTOs;

namespace ClassroomService.Domain.Messages;

/// <summary>
/// Response message for student account creation
/// </summary>
public class StudentCreationResponse
{
    public string CorrelationId { get; set; } = string.Empty;
    public CreateStudentAccountsResponse Response { get; set; } = new();
    public DateTime RespondedAt { get; set; } = DateTime.UtcNow;
}
