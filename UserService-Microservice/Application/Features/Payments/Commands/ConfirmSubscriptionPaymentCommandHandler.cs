using System;
using System.Globalization;
using System.Net;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using UserService.Application.Common.Models;
using UserService.Application.Features.Payments.Helpers;
using UserService.Application.Features.Subscriptions.Commands;
using UserService.Domain.Entities;
using UserService.Domain.Enums;
using UserService.Domain.Interfaces;
using UserService.Infrastructure.Repositories;
using UserService.Infrastructure.Services;

namespace UserService.Application.Features.Payments.Commands;

public class ConfirmSubscriptionPaymentCommandHandler : IRequestHandler<ConfirmSubscriptionPaymentCommand, ResponseModel>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPayOSPaymentService _payOsPaymentService;
    private readonly IMediator _mediator;
    private readonly IPaymentConfirmationTokenStore _tokenStore;
    private readonly IDistributedCache _cache;
    private readonly ILogger<ConfirmSubscriptionPaymentCommandHandler> _logger;

    public ConfirmSubscriptionPaymentCommandHandler(
        IUnitOfWork unitOfWork,
        IPayOSPaymentService payOsPaymentService,
        IMediator mediator,
        IPaymentConfirmationTokenStore tokenStore,
        IDistributedCache cache,
        ILogger<ConfirmSubscriptionPaymentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _payOsPaymentService = payOsPaymentService;
        _mediator = mediator;
        _tokenStore = tokenStore;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ResponseModel> Handle(ConfirmSubscriptionPaymentCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OrderCode) || string.IsNullOrWhiteSpace(request.Token))
        {
            return new ResponseModel(HttpStatusCode.BadRequest, "Order code and token are required");
        }

        var payment = await _unitOfWork.SubscriptionPayments
            .GetSingleByPropertyAsync(x => x.OrderCode!, request.OrderCode, cancellationToken);

        if (payment == null || payment.UserId != request.UserId)
        {
            return new ResponseModel(HttpStatusCode.NotFound, "Payment not found");
        }

        var orderCode = payment.OrderCode;
        if (string.IsNullOrWhiteSpace(orderCode))
        {
            return new ResponseModel(HttpStatusCode.BadRequest, "Payment is missing an order code");
        }

        if (payment.Status == SubscriptionPaymentStatus.Paid)
        {
            await _tokenStore.RemoveTokenAsync(orderCode, cancellationToken);
            await TryUpgradeSubscriptionAsync(payment, cancellationToken);
            await InvalidateDashboardCachesAsync(cancellationToken);
            return new ResponseModel(HttpStatusCode.OK, "Payment already confirmed", BuildResponse(payment));
        }

        var tokenInfo = await _tokenStore.GetTokenAsync(orderCode, cancellationToken);
        if (tokenInfo == null)
        {
            return new ResponseModel(HttpStatusCode.BadRequest, "Confirmation token expired or not found");
        }

        if (tokenInfo.ExpiresAt <= DateTime.UtcNow)
        {
            return new ResponseModel(HttpStatusCode.BadRequest, "Confirmation token expired");
        }

        if (tokenInfo.UserId != Guid.Empty && tokenInfo.UserId != payment.UserId)
        {
            return new ResponseModel(HttpStatusCode.BadRequest, "Confirmation token does not match payment");
        }

        if (tokenInfo.PaymentId != Guid.Empty && tokenInfo.PaymentId != payment.Id)
        {
            return new ResponseModel(HttpStatusCode.BadRequest, "Confirmation token does not match payment");
        }

        if (tokenInfo.SubscriptionPlanId != Guid.Empty && tokenInfo.SubscriptionPlanId != payment.SubscriptionPlanId)
        {
            return new ResponseModel(HttpStatusCode.BadRequest, "Confirmation token does not match payment");
        }

        if (!PaymentConfirmationTokenHelper.VerifyToken(request.Token, tokenInfo.TokenHash))
        {
            return new ResponseModel(HttpStatusCode.BadRequest, "Invalid confirmation token");
        }

        if (string.IsNullOrWhiteSpace(payment.PaymentLinkId))
        {
            return new ResponseModel(HttpStatusCode.BadRequest, "Payment link is not available for verification");
        }

        var paymentLink = await _payOsPaymentService.GetPaymentLinkAsync(payment.PaymentLinkId, cancellationToken);
        if (paymentLink == null)
        {
            return new ResponseModel(HttpStatusCode.BadGateway, "Unable to verify payment with PayOS");
        }

        if (!string.Equals(paymentLink.Status, "PAID", StringComparison.OrdinalIgnoreCase))
        {
            var message = $"Payment is currently {paymentLink.Status ?? "UNPAID"}";
            return new ResponseModel(HttpStatusCode.Accepted, message, BuildResponse(payment));
        }

        await MarkPaymentAsPaidAsync(payment, paymentLink, cancellationToken);
        await _tokenStore.RemoveTokenAsync(orderCode, cancellationToken);
        await TryUpgradeSubscriptionAsync(payment, cancellationToken);
        await InvalidateDashboardCachesAsync(cancellationToken);

        return new ResponseModel(HttpStatusCode.OK, "Payment confirmed", BuildResponse(payment));
    }

    private async Task MarkPaymentAsPaidAsync(SubscriptionPayment payment, Infrastructure.Services.Models.PayOSPaymentLinkResponse paymentLink, CancellationToken cancellationToken)
    {
        payment.Status = SubscriptionPaymentStatus.Paid;
        payment.PaidAt ??= DateTime.UtcNow;
        payment.PayOSPayload = paymentLink.RawPayload ?? payment.PayOSPayload;
        payment.PaymentReference ??= paymentLink.OrderCode;
        payment.Signature ??= "token-flow";

        await _unitOfWork.SubscriptionPayments.UpdateAsync(payment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task TryUpgradeSubscriptionAsync(SubscriptionPayment payment, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(payment.UserId, cancellationToken);
        if (user == null)
        {
            _logger.LogError("Subscription payment {PaymentId} references missing user {UserId}", payment.Id, payment.UserId);
            return;
        }

        // Fetch the plan for the payment to check tier
        var paymentPlan = await _unitOfWork.SubscriptionPlans.GetByIdAsync(payment.SubscriptionPlanId, cancellationToken);
        if (paymentPlan == null)
        {
            _logger.LogError("Subscription payment {PaymentId} references missing plan {PlanId}", payment.Id, payment.SubscriptionPlanId);
            return;
        }

        var isRenewal = user.SubscriptionTier?.Id == paymentPlan.Tier?.Id;
        if (!isRenewal && user.SubscriptionTier?.Level >= paymentPlan.Tier?.Level)
        {
            _logger.LogInformation("User {UserId} already at tier {Tier}, skipping confirmation upgrade for payment {PaymentId}", user.Id, user.SubscriptionTier?.Level, payment.Id);
            return;
        }

        var upgradeCommand = new UpgradeSubscriptionCommand
        {
            UserId = user.Id,
            SubscriptionPlanId = payment.SubscriptionPlanId,
            PaymentReference = payment.PaymentReference ?? payment.OrderCode,
            IsRenewal = isRenewal
        };

        try
        {
            var upgradeResponse = await _mediator.Send(upgradeCommand, cancellationToken);
            if (upgradeResponse.Status != HttpStatusCode.OK)
            {
                _logger.LogWarning("Upgrade via confirmation for user {UserId} returned {Status}: {Message}",
                    user.Id, upgradeResponse.Status, upgradeResponse.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upgrade failed for user {UserId} via confirmation token", user.Id);
        }
    }

    private static object BuildResponse(SubscriptionPayment payment)
    {
        return new
        {
            paymentId = payment.Id,
            status = payment.Status.ToString(),
            orderCode = payment.OrderCode,
            paidAt = payment.PaidAt,
            expiresAt = payment.ExpiredAt
        };
    }

    private async Task InvalidateDashboardCachesAsync(CancellationToken cancellationToken)
    {
        var dateRanges = new[]
        {
            (DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, "7days"),
            (DateTime.UtcNow.AddDays(-30), DateTime.UtcNow, "30days"),
            (DateTime.UtcNow.AddMonths(-3), DateTime.UtcNow, "3months"),
            (DateTime.UtcNow.AddMonths(-6), DateTime.UtcNow, "6months"),
            (DateTime.UtcNow.AddYears(-1), DateTime.UtcNow, "1year"),
            (DateTime.UtcNow.AddYears(-2), DateTime.UtcNow, "2years")
        };

        var quotaThresholds = new[] { 50, 60, 70, 75, 80, 85, 90 };
        var intervals = new[] { "day", "week", "month" };

        var tasks = new List<Task>();

        foreach (var (startDate, endDate, label) in dateRanges)
        {
            var startKey = startDate.ToString("yyyy-MM-dd");
            var endKey = endDate.ToString("yyyy-MM-dd");

            // User statistics cache keys (with quota threshold and interval variations)
            foreach (var threshold in quotaThresholds)
            {
                foreach (var interval in intervals)
                {
                    var userKey = $"admin:dashboard:users:{startKey}:{endKey}:{threshold}:{interval}";
                    tasks.Add(_cache.RemoveAsync(userKey, cancellationToken));
                }
            }

            // Subscription statistics cache keys (with interval variations)
            foreach (var interval in intervals)
            {
                var subKey = $"admin:dashboard:subscriptions:{startKey}:{endKey}:{interval}";
                tasks.Add(_cache.RemoveAsync(subKey, cancellationToken));
            }
        }

        await Task.WhenAll(tasks);
        _logger.LogInformation("Successfully invalidated {Count} dashboard cache keys after payment confirmation", tasks.Count);
    }
}
