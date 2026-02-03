namespace UserService.Domain.Common;

public class KafkaSettings
{
    public const string SectionName = "KafkaSettings";
    
    public string BootstrapServers { get; set; } = "localhost:9092";
    
    // Consumer group
    public string ConsumerGroup { get; set; } = "user-service-consumer";
    
    // Cache invalidation topic (User → Classroom)
    public string UserCacheInvalidationTopic { get; set; } = "user.cache.invalidation";

    // Quota usage topic auto-created if missing
    public string QuotaUsageTopic { get; set; } = "crawler.quota.usage";

    // Quota consumer group override
    public string QuotaUsageConsumerGroup { get; set; } = "user-service-quota-usage";

    // Toggle quota consumer when Kafka unavailable
    public bool EnableQuotaUsageConsumer { get; set; } = true;
}
