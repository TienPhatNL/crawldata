using System.Text.Json.Serialization;
using ClassroomService.Domain.Enums;
using MediatR;

namespace ClassroomService.Application.Features.SupportRequests.Commands;

public class RejectSupportRequestCommand : IRequest<RejectSupportRequestResponse>
{
    public Guid SupportRequestId { get; set; }
    [JsonIgnore]
    public Guid StaffId { get; set; }
    public SupportRequestRejectionReason RejectionReason { get; set; }
    public string? RejectionComments { get; set; }
}
