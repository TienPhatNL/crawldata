namespace UserService.Domain.Interfaces;

public interface IWebCrawlerQuotaCacheWriter
{
    Task SetQuotaAsync(
        Guid userId,
        int remainingQuota,
        int totalQuota,
        string planType,
        DateTime? resetDate,
        DateTime lastUpdated,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);
}
