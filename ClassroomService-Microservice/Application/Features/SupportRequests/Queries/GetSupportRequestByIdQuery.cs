using MediatR;

namespace ClassroomService.Application.Features.SupportRequests.Queries;

public class GetSupportRequestByIdQuery : IRequest<GetSupportRequestByIdResponse>
{
    public Guid SupportRequestId { get; set; }
    public Guid UserId { get; set; }
    public string UserRole { get; set; } = string.Empty;
}
