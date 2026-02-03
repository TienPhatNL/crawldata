using System;
using MediatR;
using UserService.Application.Common.Models;

namespace UserService.Application.Features.Payments.Commands;

public class ConfirmSubscriptionPaymentCommand : IRequest<ResponseModel>
{
    public Guid UserId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}
