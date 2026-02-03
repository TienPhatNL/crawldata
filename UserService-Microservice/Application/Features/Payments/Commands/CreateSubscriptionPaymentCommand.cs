using MediatR;
using UserService.Application.Common.Models;

namespace UserService.Application.Features.Payments.Commands;

public class CreateSubscriptionPaymentCommand : IRequest<ResponseModel>
{
    public Guid UserId { get; set; }
    public Guid SubscriptionPlanId { get; set; }
    public string? ReturnUrl { get; set; }
    public string? CancelUrl { get; set; }
}
