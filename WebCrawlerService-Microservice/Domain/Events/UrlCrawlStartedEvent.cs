using MediatR;

namespace WebCrawlerService.Domain.Events;

/// <summary>
/// Published when a single URL crawl begins
/// </summary>
public record UrlCrawlStartedEvent(
    Guid JobId,
    Guid UserId,
    string Url,
    DateTime StartedAt
) : INotification;
