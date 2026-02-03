namespace NotificationService.Domain.Common;

public class KafkaSettings
{
    public const string SectionName = "KafkaSettings";
    
    public string BootstrapServers { get; set; } = string.Empty;
    public string NotificationEventsTopic { get; set; } = "notification-events";
    public string ConsumerGroup { get; set; } = "notification-service-group";
    public int SessionTimeoutMs { get; set; } = 30000;
    public int MaxPollIntervalMs { get; set; } = 300000;
}
