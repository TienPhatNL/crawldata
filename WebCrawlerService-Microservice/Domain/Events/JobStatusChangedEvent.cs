using WebCrawlerService.Domain.Common;
using WebCrawlerService.Domain.Enums;

namespace WebCrawlerService.Domain.Events;

/// <summary>
/// Published when job status changes
/// </summary>
public class JobStatusChangedEvent : BaseEvent
{
    public Guid JobId { get; }
    public Guid UserId { get; }
    public JobStatus OldStatus { get; }
    public JobStatus NewStatus { get; }
    public DateTime ChangedAt { get; }

    public JobStatusChangedEvent(Guid jobId, Guid UserId, JobStatus oldStatus, JobStatus newStatus, DateTime changedAt)
    {
        JobId = jobId;
        this.UserId = UserId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        ChangedAt = changedAt;
    }
}
