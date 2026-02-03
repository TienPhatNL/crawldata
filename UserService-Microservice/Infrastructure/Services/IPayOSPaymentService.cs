using PayOS.Models;
using PayOS.Models.Webhooks;
using UserService.Infrastructure.Services.Models;

namespace UserService.Infrastructure.Services;

public interface IPayOSPaymentService
{
    Task<PayOSPaymentLinkResponse> CreatePaymentLinkAsync(PayOSPaymentLinkRequest request, CancellationToken cancellationToken = default);
    Task<WebhookData> VerifyWebhookAsync(Webhook webhook, CancellationToken cancellationToken = default);
    Task<PayOSPaymentLinkResponse?> GetPaymentLinkAsync(string paymentLinkId, CancellationToken cancellationToken = default);
}
