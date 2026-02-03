namespace NotificationService.Domain.Common;

public class RedisCacheSettings
{
    public const string SectionName = "Redis";
    
    public string InstanceName { get; set; } = "notificationservice:";
    public int DefaultExpirationMinutes { get; set; } = 30;
    public int NotificationListExpirationMinutes { get; set; } = 5;
    public int PreferenceExpirationMinutes { get; set; } = 60;
}
