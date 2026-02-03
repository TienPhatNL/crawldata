using MediatR;
using UserService.Application.Common.Models;

namespace UserService.Application.Features.Payments.Commands;

public class ConfirmSubscriptionPaymentFromReturnCommand : IRequest<ResponseModel>
{
    public string OrderCode { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}
