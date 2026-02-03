using System;
using System.Threading;
using System.Threading.Tasks;

namespace UserService.Domain.Interfaces;

public class PaymentConfirmationTokenInfo
{
    public string TokenHash { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid PaymentId { get; set; }
    public Guid SubscriptionPlanId { get; set; }
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow;
}

public interface IPaymentConfirmationTokenStore
{
    Task StoreTokenAsync(string orderCode, PaymentConfirmationTokenInfo info, TimeSpan ttl, CancellationToken cancellationToken = default);
    Task<PaymentConfirmationTokenInfo?> GetTokenAsync(string orderCode, CancellationToken cancellationToken = default);
    Task RemoveTokenAsync(string orderCode, CancellationToken cancellationToken = default);
}
