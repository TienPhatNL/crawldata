namespace WebCrawlerService.Domain.Common;

public class CacheSettings
{
    public const string SectionName = "CacheSettings";
    
    public TimeSpan DefaultExpiry { get; init; } = TimeSpan.FromMinutes(15);
    public TimeSpan ShortExpiry { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan LongExpiry { get; init; } = TimeSpan.FromHours(1);
    public TimeSpan UserJobsExpiry { get; init; } = TimeSpan.FromMinutes(10);
    public TimeSpan DomainPolicyExpiry { get; init; } = TimeSpan.FromHours(24);
    public string ConnectionString { get; init; } = string.Empty;
    public string InstanceName { get; init; } = "WebCrawlerService";
    public bool UseCompression { get; init; } = true;
    public bool UseDistributedCache { get; init; } = true;
}