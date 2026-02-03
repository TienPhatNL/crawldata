using MediatR;

namespace ClassroomService.Application.Features.SupportRequests.Commands;

public class CancelSupportRequestCommand : IRequest<CancelSupportRequestResponse>
{
    public Guid SupportRequestId { get; set; }
    public Guid UserId { get; set; }
}

public class CancelSupportRequestResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
