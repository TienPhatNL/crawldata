using System;
using System.Globalization;
using System.Net;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using PayOS;
using PayOS.Models.Webhooks;
using UserService.Application.Common.Models;
using UserService.Application.Features.Subscriptions.Commands;
using UserService.Domain.Entities;
using UserService.Domain.Enums;
using UserService.Infrastructure.Repositories;

namespace UserService.Application.Features.Payments.Commands;

public class HandlePayOSWebhookCommandHandler : IRequestHandler<HandlePayOSWebhookCommand, ResponseModel>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly PayOSClient _payOsClient;
    private readonly IMediator _mediator;
    private readonly ILogger<HandlePayOSWebhookCommandHandler> _logger;

    public HandlePayOSWebhookCommandHandler(
        IUnitOfWork unitOfWork,
        PayOSClient payOsClient,
        IMediator mediator,
        ILogger<HandlePayOSWebhookCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _payOsClient = payOsClient;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<ResponseModel> Handle(HandlePayOSWebhookCommand request, CancellationToken cancellationToken)
    {
        if (request.Payload == null)
        {
            return new ResponseModel(HttpStatusCode.BadRequest, "Webhook payload is empty");
        }

        if (string.IsNullOrWhiteSpace(request.Payload.Signature))
        {
            return new ResponseModel(HttpStatusCode.BadRequest, "Missing webhook signature");
        }

        var webhookData = await _payOsClient.Webhooks.VerifyAsync(request.Payload);

        var orderCode = webhookData.OrderCode.ToString(CultureInfo.InvariantCulture);

        var payment = await _unitOfWork.SubscriptionPayments
            .GetSingleByPropertyAsync(x => x.OrderCode!, orderCode, cancellationToken);

        if (payment == null)
        {
            _logger.LogWarning("PayOS webhook received for unknown orderCode {OrderCode}", orderCode);
            return new ResponseModel(HttpStatusCode.NotFound, "Payment not found");
        }

        var mappedStatus = MapStatus(request.Payload);

        if (payment.Status == SubscriptionPaymentStatus.Paid && mappedStatus == SubscriptionPaymentStatus.Paid &&
            string.Equals(payment.PaymentReference, webhookData.Reference ?? payment.PaymentReference, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("PayOS webhook for order {OrderCode} already processed", orderCode);
            return new ResponseModel(HttpStatusCode.OK, "Payment already settled", new
            {
                paymentId = payment.Id,
                status = payment.Status.ToString(),
                orderCode
            });
        }

        payment.Status = mappedStatus;
        payment.PaymentReference = webhookData.Reference ?? payment.PaymentReference;
        payment.PaymentLinkId = webhookData.PaymentLinkId ?? payment.PaymentLinkId;
        payment.PayOSPayload = JsonSerializer.Serialize(request.Payload);
        payment.Signature = request.Payload.Signature;
        payment.UpdatedAt = DateTime.UtcNow;

        if (mappedStatus == SubscriptionPaymentStatus.Paid)
        {
            payment.PaidAt = DateTime.UtcNow;
            payment.FailureReason = null;
        }
        else if (mappedStatus == SubscriptionPaymentStatus.Failed || mappedStatus == SubscriptionPaymentStatus.Cancelled)
        {
            payment.FailureReason = request.Payload.Description ?? request.Payload.Code;
            payment.CancelledAt = mappedStatus == SubscriptionPaymentStatus.Cancelled ? DateTime.UtcNow : payment.CancelledAt;
        }
        else if (mappedStatus == SubscriptionPaymentStatus.Expired)
        {
            payment.CancelledAt = DateTime.UtcNow;
        }

        await _unitOfWork.SubscriptionPayments.UpdateAsync(payment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (mappedStatus == SubscriptionPaymentStatus.Paid)
        {
            await TryUpgradeSubscriptionAsync(payment, webhookData, cancellationToken);
        }

        _logger.LogInformation("Processed PayOS webhook for payment {PaymentId} with status {Status}", payment.Id, payment.Status);

        var responseData = new
        {
            paymentId = payment.Id,
            status = payment.Status.ToString(),
            orderCode
        };

        return new ResponseModel(HttpStatusCode.OK, "Webhook processed", responseData);
    }

    private async Task TryUpgradeSubscriptionAsync(SubscriptionPayment payment, WebhookData webhookData, CancellationToken cancellationToken)
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
            _logger.LogInformation("User {UserId} already at tier {Tier}, skipping auto-upgrade for payment {PaymentId}", user.Id, user.SubscriptionTier?.Name, payment.Id);
            return;
        }

        var upgradeCommand = new UpgradeSubscriptionCommand
        {
            UserId = user.Id,
            SubscriptionPlanId = payment.SubscriptionPlanId,
            PaymentReference = payment.PaymentReference ?? webhookData.Reference ?? payment.OrderCode,
            IsRenewal = isRenewal
        };

        try
        {
            var upgradeResponse = await _mediator.Send(upgradeCommand, cancellationToken);
            if (upgradeResponse.Status != HttpStatusCode.OK)
            {
                _logger.LogWarning("Auto-upgrade for user {UserId} via payment {PaymentId} returned status {Status}: {Message}",
                    user.Id, payment.Id, upgradeResponse.Status, upgradeResponse.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto-upgrade failed for user {UserId} via payment {PaymentId}", user.Id, payment.Id);
        }
    }

    private static SubscriptionPaymentStatus MapStatus(Webhook webhook)
    {
        if (webhook.Success)
        {
            return SubscriptionPaymentStatus.Paid;
        }

        return webhook.Code?.Trim() switch
        {
            "09" => SubscriptionPaymentStatus.Cancelled,
            "10" => SubscriptionPaymentStatus.Expired,
            "01" => SubscriptionPaymentStatus.Processing,
            _ => SubscriptionPaymentStatus.Failed
        };
    }
}
