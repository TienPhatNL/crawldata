using System.Net;
using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.Common.Models;
using UserService.Infrastructure.Repositories;

namespace UserService.Application.Features.Payments.Commands;

public class ConfirmSubscriptionPaymentFromReturnCommandHandler : IRequestHandler<ConfirmSubscriptionPaymentFromReturnCommand, ResponseModel>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;
    private readonly ILogger<ConfirmSubscriptionPaymentFromReturnCommandHandler> _logger;

    public ConfirmSubscriptionPaymentFromReturnCommandHandler(
        IUnitOfWork unitOfWork,
        IMediator mediator,
        ILogger<ConfirmSubscriptionPaymentFromReturnCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<ResponseModel> Handle(ConfirmSubscriptionPaymentFromReturnCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OrderCode) || string.IsNullOrWhiteSpace(request.Token))
        {
            return new ResponseModel(HttpStatusCode.BadRequest, "Order code and token are required");
        }

        var orderCode = request.OrderCode.Trim();
        var payment = await _unitOfWork.SubscriptionPayments
            .GetSingleByPropertyAsync(x => x.OrderCode!, orderCode, cancellationToken);

        if (payment == null)
        {
            _logger.LogWarning("PayOS return confirmation failed: payment not found for orderCode {OrderCode}", orderCode);
            return new ResponseModel(HttpStatusCode.NotFound, "Payment not found");
        }

        var confirmCommand = new ConfirmSubscriptionPaymentCommand
        {
            UserId = payment.UserId,
            OrderCode = orderCode,
            Token = request.Token
        };

        return await _mediator.Send(confirmCommand, cancellationToken);
    }
}
