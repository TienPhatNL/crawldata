using MediatR;

namespace WebCrawlerService.Domain.Common;

public abstract class BaseEvent : INotification
{
    public DateTime DateOccurred { get; protected set; } = DateTime.UtcNow;
    public string EventType { get; protected set; } = string.Empty;

    protected BaseEvent()
    {
        EventType = GetType().Name;
    }
}