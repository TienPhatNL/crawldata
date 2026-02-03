using UserService.Infrastructure.Services.Models;

namespace UserService.Infrastructure.Services;

public interface IPayOSRelayClient
{
    Task<PayOSPaymentLinkResponse?> CreatePaymentLinkAsync(PayOSPaymentLinkRequest request, CancellationToken cancellationToken = default);
}
