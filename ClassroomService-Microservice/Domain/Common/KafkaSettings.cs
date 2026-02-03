namespace ClassroomService.Domain.Common;

public class KafkaSettings
{
    public const string SectionName = "KafkaSettings";
    
    public string BootstrapServers { get; set; } = "localhost:9092";
    
    // Request topics (Classroom → User)
    public string UserQueryRequestTopic { get; init; } = "classroom.user.query.request";
    public string UserValidationRequestTopic { get; init; } = "classroom.user.validation.request";
    public string StudentCreationRequestTopic { get; init; } = "classroom.student.creation.request";

    // Crawler topics (Classroom → WebCrawler)
    public string SmartCrawlRequestTopic { get; init; } = "classroom.crawler.request";
    
    // Response topics (User → Classroom)
    public string UserQueryResponseTopic { get; init; } = "classroom.user.query.response";
    public string UserValidationResponseTopic { get; init; } = "classroom.user.validation.response";
    public string StudentCreationResponseTopic { get; init; } = "classroom.student.creation.response";
    
    // Cache invalidation topic (User → Classroom)
    public string UserCacheInvalidationTopic { get; init; } = "user.cache.invalidation";
    
    // Consumer group
    public string ConsumerGroup { get; init; } = "classroom-service-consumer";
    
    // Request timeout
    public int RequestTimeoutSeconds { get; init; } = 30;
    
    // Dead letter queue
    public string DeadLetterTopic { get; init; } = "classroom.dlq";
}
