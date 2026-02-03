namespace ClassroomService.Application.Features.SupportRequests.Commands;

public class RejectSupportRequestResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
