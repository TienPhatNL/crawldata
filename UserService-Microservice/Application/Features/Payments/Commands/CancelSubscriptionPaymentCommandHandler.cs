using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using UserService.Application.Common.Models;
using UserService.Application.Features.Subscriptions.Commands;
using UserService.Domain.Enums;
using UserService.Domain.Interfaces;
using UserService.Infrastructure.Repositories;

namespace UserService.Application.Features.Payments.Commands;

public class CancelSubscriptionPaymentCommandHandler : IRequestHandler<CancelSubscriptionPaymentCommand, ResponseModel>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CancelSubscriptionPaymentCommandHandler> _logger;

    public CancelSubscriptionPaymentCommandHandler(
        IUnitOfWork unitOfWork,
        IMediator mediator,
        IDistributedCache cache,
        ILogger<CancelSubscriptionPaymentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mediator = mediator;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ResponseModel> Handle(CancelSubscriptionPaymentCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OrderCode))
        {
            return new ResponseModel(HttpStatusCode.BadRequest, "Order code is required");
        }

        var payment = await _unitOfWork.SubscriptionPayments
            .GetSingleByPropertyAsync(x => x.OrderCode!, request.OrderCode, cancellationToken);

        if (payment == null || payment.UserId != request.UserId)
        {
            return new ResponseModel(HttpStatusCode.NotFound, "Payment not found or unauthorized");
        }

        // Only allow cancellation of Pending or Processing payments
        if (payment.Status != SubscriptionPaymentStatus.Pending && 
            payment.Status != SubscriptionPaymentStatus.Processing)
        {
            return new ResponseModel(
                HttpStatusCode.BadRequest, 
                $"Cannot cancel payment with status: {payment.Status}");
        }

        // Update payment status
        payment.Status = SubscriptionPaymentStatus.Cancelled;
        payment.CancelledAt = DateTime.UtcNow;
        payment.FailureReason = string.IsNullOrWhiteSpace(request.Reason) 
            ? "Cancelled by user" 
            : $"Cancelled by user: {request.Reason}";

        await _unitOfWork.SubscriptionPayments.UpdateAsync(payment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Payment {OrderCode} cancelled by user {UserId}. Reason: {Reason}",
            request.OrderCode, request.UserId, payment.FailureReason);

        return new ResponseModel(
            HttpStatusCode.OK, 
            "Payment cancelled successfully",
            new
            {
                OrderCode = payment.OrderCode,
                Status = payment.Status.ToString(),
                CancelledAt = payment.CancelledAt
            });
    }

    private async Task InvalidateDashboardCachesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _cache.RemoveAsync("admin:dashboard:subscription-payments", cancellationToken);
            await _cache.RemoveAsync("admin:dashboard:payment-summary", cancellationToken);
            await _cache.RemoveAsync("admin:dashboard:tier-distribution", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate dashboard caches");
        }
    }
}
