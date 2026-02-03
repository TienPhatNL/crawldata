namespace WebCrawlerService.Application.Common.Exceptions;

public abstract class CrawlerException : Exception
{
    protected CrawlerException(string message) : base(message)
    {
    }

    protected CrawlerException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class CrawlJobNotFoundException : CrawlerException
{
    public CrawlJobNotFoundException(Guid jobId) 
        : base($"Crawl job with ID '{jobId}' was not found.")
    {
    }
}

public class CrawlerAgentNotFoundException : CrawlerException
{
    public CrawlerAgentNotFoundException(Guid agentId) 
        : base($"Crawler agent with ID '{agentId}' was not found.")
    {
    }
}

public class DomainPolicyViolationException : CrawlerException
{
    public string Domain { get; }

    public DomainPolicyViolationException(string domain) 
        : base($"Domain '{domain}' is not allowed for crawling.")
    {
        Domain = domain;
    }
}

public class QuotaExceededException : CrawlerException
{
    public Guid UserId { get; }
    public string QuotaType { get; }

    public QuotaExceededException(Guid userId, string quotaType) 
        : base($"User '{userId}' has exceeded their {quotaType} quota.")
    {
        UserId = userId;
        QuotaType = quotaType;
    }
}

public class CrawlerUnavailableException : CrawlerException
{
    public CrawlerUnavailableException(string crawlerType) 
        : base($"No {crawlerType} crawlers are currently available.")
    {
    }
}

public class InvalidCrawlConfigurationException : CrawlerException
{
    public InvalidCrawlConfigurationException(string message) 
        : base($"Invalid crawl configuration: {message}")
    {
    }
}

public class CrawlJobAlreadyProcessingException : CrawlerException
{
    public CrawlJobAlreadyProcessingException(Guid jobId) 
        : base($"Crawl job with ID '{jobId}' is already being processed.")
    {
    }
}

public class UnauthorizedCrawlAccessException : CrawlerException
{
    public UnauthorizedCrawlAccessException(Guid userId, Guid jobId) 
        : base($"User '{userId}' is not authorized to access crawl job '{jobId}'.")
    {
    }
}