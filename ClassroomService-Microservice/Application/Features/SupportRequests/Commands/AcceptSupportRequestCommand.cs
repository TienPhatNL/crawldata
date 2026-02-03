using MediatR;

namespace ClassroomService.Application.Features.SupportRequests.Commands;

public class AcceptSupportRequestCommand : IRequest<AcceptSupportRequestResponse>
{
    public Guid SupportRequestId { get; set; }
    public Guid StaffId { get; set; }
}

public class AcceptSupportRequestResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? ConversationId { get; set; }
}
