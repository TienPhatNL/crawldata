namespace WebCrawlerService.Application.Common.Caching;

public static class CacheKeys
{
    public const string CrawlJobPrefix = "crawljob";
    public const string UserJobsPrefix = "userjobs";
    public const string CrawlerAgentPrefix = "crawleragent";
    public const string DomainPolicyPrefix = "domainpolicy";
    public const string CrawlResultsPrefix = "crawlresults";
    
    // Cache key builders
    public static string CrawlJob(Guid jobId) => $"{CrawlJobPrefix}:{jobId}";
    public static string UserJobs(Guid userId, int page = 1, int size = 10) => $"{UserJobsPrefix}:{userId}:p{page}s{size}";
    public static string CrawlerAgent(Guid agentId) => $"{CrawlerAgentPrefix}:{agentId}";
    public static string DomainPolicies() => $"{DomainPolicyPrefix}:all";
    public static string CrawlResults(Guid jobId) => $"{CrawlResultsPrefix}:{jobId}";
    
    // Pattern builders for bulk operations
    public static string UserJobsPattern(Guid userId) => $"{UserJobsPrefix}:{userId}:*";
    public static string CrawlJobPattern(Guid jobId) => $"{CrawlJobPrefix}:{jobId}*";
}