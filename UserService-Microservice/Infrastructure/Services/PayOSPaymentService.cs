using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PayOS;
using PayOS.Models;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.V2.PaymentRequests.Invoices;
using PayOS.Models.Webhooks;
using UserService.Infrastructure.Configuration;
using UserService.Infrastructure.Services.Models;

namespace UserService.Infrastructure.Services;

public class PayOSPaymentService : IPayOSPaymentService
{
    private readonly PayOSClient _client;
    private readonly PayOSSettings _settings;
    private readonly ILogger<PayOSPaymentService> _logger;
    private readonly IPayOSRelayClient _relayClient;

    public PayOSPaymentService(
        PayOSClient client,
        IOptions<PayOSSettings> options,
        ILogger<PayOSPaymentService> logger,
        IPayOSRelayClient relayClient)
    {
        _client = client;
        _settings = options.Value;
        _logger = logger;
        _relayClient = relayClient;
    }

    public async Task<PayOSPaymentLinkResponse> CreatePaymentLinkAsync(PayOSPaymentLinkRequest request, CancellationToken cancellationToken = default)
    {
        var amount = Convert.ToInt64(Math.Round(request.Amount));

        var createRequest = new CreatePaymentLinkRequest
        {
            OrderCode = request.OrderCode,
            Amount = amount,
            Description = request.Description,
            ReturnUrl = string.IsNullOrWhiteSpace(request.ReturnUrl) ? _settings.ReturnUrl : request.ReturnUrl,
            CancelUrl = string.IsNullOrWhiteSpace(request.CancelUrl) ? _settings.CancelUrl : request.CancelUrl,
            BuyerName = request.BuyerName,
            BuyerEmail = request.BuyerEmail,
            BuyerPhone = request.BuyerPhone,
            BuyerCompanyName = request.BuyerCompanyName,
            BuyerAddress = request.BuyerAddress,
            ExpiredAt = request.ExpiredAt?.ToUnixTimeSeconds()
        };

        if (request.Items != null && request.Items.Count > 0)
        {
            createRequest.Items = request.Items.Select(item => new PaymentLinkItem
            {
                Name = item.Name,
                Quantity = item.Quantity,
                Price = Convert.ToInt64(Math.Round(item.Price)),
                Unit = item.Unit
            }).ToList();
        }

        if (request.BuyerNotGetInvoice.HasValue)
        {
            createRequest.Invoice = new InvoiceRequest
            {
                BuyerNotGetInvoice = request.BuyerNotGetInvoice
            };
        }

        _logger.LogInformation("Creating PayOS payment link for order {OrderCode}", request.OrderCode);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var response = await _client.PaymentRequests.CreateAsync(createRequest);
            return MapResponse(response);
        }
        catch (Exception ex) when (ShouldAttemptRelay(ex))
        {
            _logger.LogWarning(ex, "Primary PayOS call failed for order {OrderCode}. Attempting relay fallback.", request.OrderCode);
            var fallbackResponse = await TryCreateViaRelayAsync(request, cancellationToken);
            if (fallbackResponse != null)
            {
                return fallbackResponse;
            }

            _logger.LogError(ex, "Relay fallback unavailable for order {OrderCode}", request.OrderCode);
            throw;
        }
    }

    private PayOSPaymentLinkResponse MapResponse(CreatePaymentLinkResponse response)
    {
        var serialized = JsonSerializer.Serialize(response);
        DateTime? expiresAt = null;
        if (response.ExpiredAt is long expiredAt && expiredAt > 0)
        {
            expiresAt = DateTimeOffset.FromUnixTimeMilliseconds(expiredAt).UtcDateTime;
        }

        return new PayOSPaymentLinkResponse
        {
            PaymentLinkId = response.PaymentLinkId ?? string.Empty,
            OrderCode = response.OrderCode.ToString(CultureInfo.InvariantCulture),
            CheckoutUrl = response.CheckoutUrl ?? string.Empty,
            QrCode = response.QrCode,
            Status = response.Status.ToString(),
            ExpiredAt = expiresAt,
            RawPayload = serialized
        };
    }

    private async Task<PayOSPaymentLinkResponse?> TryCreateViaRelayAsync(PayOSPaymentLinkRequest request, CancellationToken cancellationToken)
    {
        if (!_settings.EnableRelayFallback)
        {
            return null;
        }

        try
        {
            var relayResponse = await _relayClient.CreatePaymentLinkAsync(request, cancellationToken);
            if (relayResponse != null)
            {
                _logger.LogInformation("Relay fallback succeeded for order {OrderCode}", request.OrderCode);
            }
            return relayResponse;
        }
        catch (Exception relayEx)
        {
            _logger.LogError(relayEx, "Relay fallback failed for order {OrderCode}", request.OrderCode);
            return null;
        }
    }

    private static bool ShouldAttemptRelay(Exception exception)
    {
        if (exception is HttpRequestException || exception is SocketException)
        {
            return true;
        }

        var baseException = exception.GetBaseException();
        return baseException is HttpRequestException || baseException is SocketException;
    }

    public async Task<WebhookData> VerifyWebhookAsync(Webhook webhook, CancellationToken cancellationToken = default)
    {
        if (webhook == null)
        {
            throw new ArgumentNullException(nameof(webhook));
        }

        _logger.LogInformation("Verifying PayOS webhook for order {OrderCode}", webhook.Data?.OrderCode);
        cancellationToken.ThrowIfCancellationRequested();
        return await _client.Webhooks.VerifyAsync(webhook);
    }

    public async Task<PayOSPaymentLinkResponse?> GetPaymentLinkAsync(string paymentLinkId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(paymentLinkId))
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var paymentLink = await _client.PaymentRequests.GetAsync(paymentLinkId);
        return paymentLink == null ? null : MapPaymentLink(paymentLink);
    }

    private PayOSPaymentLinkResponse MapPaymentLink(PaymentLink paymentLink)
    {
        var serialized = JsonSerializer.Serialize(paymentLink);
        return new PayOSPaymentLinkResponse
        {
            OrderCode = paymentLink.OrderCode.ToString(CultureInfo.InvariantCulture),
            Status = paymentLink.Status.ToString(),
            RawPayload = serialized
        };
    }
}
