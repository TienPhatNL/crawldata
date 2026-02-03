using ClassroomService.Domain.DTOs;

namespace ClassroomService.Domain.Messages;

/// <summary>
/// Request message for creating student accounts
/// </summary>
public class StudentCreationRequest
{
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    public CreateStudentAccountsRequest Request { get; set; } = new();
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}
