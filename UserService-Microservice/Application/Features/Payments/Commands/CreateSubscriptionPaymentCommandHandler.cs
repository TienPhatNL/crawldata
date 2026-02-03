using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UserService.Application.Common.Models;
using UserService.Application.Features.Payments.Helpers;
using UserService.Domain.Entities;
using UserService.Domain.Enums;
using UserService.Domain.Interfaces;
using UserService.Infrastructure.Configuration;
using UserService.Infrastructure.Repositories;
using UserService.Infrastructure.Services;
using UserService.Infrastructure.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace UserService.Application.Features.Payments.Commands;

public class CreateSubscriptionPaymentCommandHandler : IRequestHandler<CreateSubscriptionPaymentCommand, ResponseModel>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPayOSPaymentService _payOsPaymentService;
    private readonly IPaymentConfirmationTokenStore _tokenStore;
    private readonly PayOSSettings _payOsSettings;
    private readonly ILogger<CreateSubscriptionPaymentCommandHandler> _logger;

    public CreateSubscriptionPaymentCommandHandler(
        IUnitOfWork unitOfWork,
        IPayOSPaymentService payOsPaymentService,
        IPaymentConfirmationTokenStore tokenStore,
        IOptions<PayOSSettings> payOsOptions,
        ILogger<CreateSubscriptionPaymentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _payOsPaymentService = payOsPaymentService;
        _tokenStore = tokenStore;
        _payOsSettings = payOsOptions.Value;
        _logger = logger;
    }

    public async Task<ResponseModel> Handle(CreateSubscriptionPaymentCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return new ResponseModel(HttpStatusCode.NotFound, "User not found");
        }

        if (user.IsDeleted)
        {
            return new ResponseModel(HttpStatusCode.BadRequest, "User has been deleted");
        }

        // Fetch plan from database by ID
        var selectedPlan = await _unitOfWork.SubscriptionPlans.GetByIdAsync(request.SubscriptionPlanId, cancellationToken);
        
        if (selectedPlan == null)
        {
            return new ResponseModel(HttpStatusCode.NotFound, "Subscription plan not found");
        }

        if (selectedPlan.IsDeleted || !selectedPlan.IsActive)
        {
            return new ResponseModel(HttpStatusCode.BadRequest, "Subscription plan is not available");
        }

        if (selectedPlan.Tier?.Level == 0 || selectedPlan.Price <= 0)
        {
            return new ResponseModel(HttpStatusCode.BadRequest, "Cannot create payment link for free or zero-price plans");
        }

        

        var amount = selectedPlan.Price;
        var currency = string.IsNullOrWhiteSpace(selectedPlan.Currency) ? "VND" : selectedPlan.Currency;
        var orderCode = GenerateOrderCode();
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(30);

        var description = BuildPayOsDescription(selectedPlan.Name);
        var (confirmationToken, confirmationTokenHash) = PaymentConfirmationTokenHelper.CreateToken();
        var tokenTtlMinutes = _payOsSettings.ConfirmationTokenTtlMinutes <= 0 ? 15 : _payOsSettings.ConfirmationTokenTtlMinutes;
        var tokenIssuedAt = DateTime.UtcNow;
        var tokenExpiresAt = tokenIssuedAt.AddMinutes(tokenTtlMinutes);
        var resolvedReturnUrl = ResolveUrl(request.ReturnUrl, _payOsSettings.ReturnUrl);
        var resolvedCancelUrl = ResolveUrl(request.CancelUrl, _payOsSettings.CancelUrl);

        var createRequest = new PayOSPaymentLinkRequest
        {
            OrderCode = orderCode,
            Amount = amount,
            Description = description,
            ReturnUrl = AppendConfirmationParams(resolvedReturnUrl, confirmationToken, orderCode),
            CancelUrl = AppendConfirmationParams(resolvedCancelUrl, confirmationToken, orderCode),
            BuyerName = user.FullName,
            BuyerEmail = user.Email,
            BuyerPhone = user.PhoneNumber,
            BuyerCompanyName = user.InstitutionName,
            BuyerAddress = user.InstitutionAddress,
            ExpiredAt = expiresAt,
            BuyerNotGetInvoice = true,
            Items = new List<PayOSPaymentItem>
            {
                new()
                {
                    Name = $"{selectedPlan.Name} subscription",
                    Quantity = 1,
                    Price = amount,
                    Unit = "subscription"
                }
            }
        };

        _logger.LogInformation(
            "Creating PayOS subscription payment for user {UserId} with ReturnUrl {ReturnUrl} and CancelUrl {CancelUrl}",
            user.Id,
            createRequest.ReturnUrl,
            createRequest.CancelUrl);

        var payOsResponse = await _payOsPaymentService.CreatePaymentLinkAsync(createRequest, cancellationToken);
        var normalizedOrderCode = string.IsNullOrWhiteSpace(payOsResponse.OrderCode)
            ? orderCode.ToString(CultureInfo.InvariantCulture)
            : payOsResponse.OrderCode;
        var serializedResponse = payOsResponse.RawPayload;

        var payment = new SubscriptionPayment
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SubscriptionPlanId = selectedPlan.Id,
            Status = SubscriptionPaymentStatus.Pending,
            Amount = amount,
            Currency = currency,
            PaymentLinkId = payOsResponse.PaymentLinkId ?? string.Empty,
            OrderCode = normalizedOrderCode,
            CheckoutUrl = payOsResponse.CheckoutUrl ?? string.Empty,
            ExpiredAt = payOsResponse.ExpiredAt,
            PayOSPayload = serializedResponse,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.SubscriptionPayments.AddAsync(payment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var tokenInfo = new PaymentConfirmationTokenInfo
        {
            TokenHash = confirmationTokenHash,
            UserId = user.Id,
            PaymentId = payment.Id,
            SubscriptionPlanId = selectedPlan.Id,
            IssuedAt = tokenIssuedAt,
            ExpiresAt = tokenExpiresAt
        };
        var tokenTtl = TimeSpan.FromMinutes(tokenTtlMinutes);
        await _tokenStore.StoreTokenAsync(normalizedOrderCode, tokenInfo, tokenTtl, cancellationToken);

        _logger.LogInformation("Created subscription payment {PaymentId} for user {UserId}" , payment.Id, user.Id);

        var responseData = new
        {
            paymentId = payment.Id,
            paymentLinkId = payment.PaymentLinkId,
            orderCode = normalizedOrderCode,
            checkoutUrl = payment.CheckoutUrl,
            qrCode = payOsResponse.QrCode,
            amount,
            currency,
            expiresAt = payment.ExpiredAt,
            confirmationToken
        };

        return new ResponseModel(HttpStatusCode.OK, "Payment link created", responseData);
    }

    private static long GenerateOrderCode()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var suffix = RandomNumberGenerator.GetInt32(100, 999);
        var composed = $"{timestamp}{suffix}";
        return long.Parse(composed, CultureInfo.InvariantCulture);
    }

    private static string BuildPayOsDescription(string planName)
    {
        var text = $"CrawlData {planName} sub";
        return text.Length <= 32 ? text : text[..32];
    }

    private static string ResolveUrl(string? requestedUrl, string? fallback)
    {
        return string.IsNullOrWhiteSpace(requestedUrl)
            ? (string.IsNullOrWhiteSpace(fallback) ? string.Empty : fallback)
            : requestedUrl;
    }

    private static string AppendConfirmationParams(string? baseUrl, string token, long orderCode)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return string.Empty;
        }

        var parameters = new Dictionary<string, string?>
        {
            ["confirmationToken"] = token,
            ["orderCode"] = orderCode.ToString(CultureInfo.InvariantCulture)
        };

        return QueryHelpers.AddQueryString(baseUrl, parameters);
    }
}
