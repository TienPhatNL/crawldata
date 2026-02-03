namespace WebCrawlerService.Application.Common.Interfaces;

public interface IDateTimeService
{
    DateTime UtcNow { get; }
    DateTime Now { get; }
    DateOnly Today { get; }
}