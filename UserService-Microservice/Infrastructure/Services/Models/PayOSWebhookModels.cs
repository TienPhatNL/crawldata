using System.Text.Json;

namespace UserService.Infrastructure.Services.Models;

public class PayOSWebhookRequest
{
    public JsonElement Data { get; set; }
    public string Signature { get; set; } = string.Empty;
}

public class PayOSWebhookData
{
    public string? OrderCode { get; set; }
    public string? Status { get; set; }
    public long Amount { get; set; }
    public string? TransactionId { get; set; }
    public string? PaymentLinkId { get; set; }
    public DateTime? PaidAt { get; set; }
}
