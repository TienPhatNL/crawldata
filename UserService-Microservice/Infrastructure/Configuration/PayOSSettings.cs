namespace UserService.Infrastructure.Configuration;

public class PayOSSettings
{
    public const string SectionName = "PayOS";

    public string ClientId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ChecksumKey { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
    public bool EnableRelayFallback { get; set; }
    public string RelayBaseUrl { get; set; } = string.Empty;
    public string RelayEndpoint { get; set; } = "/api/payments/create";
    public string RelayApiKey { get; set; } = string.Empty;
    public int RelayTimeoutSeconds { get; set; } = 10;
    public int ConfirmationTokenTtlMinutes { get; set; } = 15;
}
