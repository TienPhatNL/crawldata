using MediatR;

namespace ClassroomService.Application.Features.SupportRequests.Commands;

public class ResolveSupportRequestCommand : IRequest<ResolveSupportRequestResponse>
{
    public Guid SupportRequestId { get; set; }
    public Guid UserId { get; set; }
}

public class ResolveSupportRequestResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
