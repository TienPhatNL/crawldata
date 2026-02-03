using MediatR;
using UserService.Application.Common.Models;

namespace UserService.Application.Features.Subscriptions.Commands;

public class CancelSubscriptionCommand : IRequest<ResponseModel>
{
    public Guid UserId { get; set; }
    public string? Reason { get; set; }
    public DateTime? EffectiveDate { get; set; }
}