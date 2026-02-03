namespace WebCrawlerService.Domain.Interfaces;

public interface IDateTimeService
{
    DateTime UtcNow { get; }
    DateTime Now { get; }
    DateOnly Today { get; }
}