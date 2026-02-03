using MediatR;
using PayOS.Models.Webhooks;
using UserService.Application.Common.Models;

namespace UserService.Application.Features.Payments.Commands;

public class HandlePayOSWebhookCommand : IRequest<ResponseModel>
{
    public Webhook? Payload { get; set; }
}
