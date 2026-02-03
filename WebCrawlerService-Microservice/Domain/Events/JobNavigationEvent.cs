using WebCrawlerService.Domain.Common;

namespace WebCrawlerService.Domain.Events;

/// <summary>
/// Published when navigation planning or execution events occur during intelligent crawling
/// </summary>
public class JobNavigationEvent : BaseEvent
{
    public Guid JobId { get; }
    public Guid UserId { get; }
    public string NavigationEventType { get; } // NavigationPlanningStarted, NavigationPlanningCompleted, etc.
    public int? StepNumber { get; }
    public int? TotalSteps { get; }
    public string? Action { get; } // click, select, scroll, wait
    public string? Description { get; }
    public string? CurrentUrl { get; }
    public string? TargetElement { get; }
    public DateTime OccurredAt { get; }

    public JobNavigationEvent(
        Guid jobId, 
        Guid userId, 
        string navigationEventType,
        int? stepNumber = null,
        int? totalSteps = null,
        string? action = null,
        string? description = null,
        string? currentUrl = null,
        string? targetElement = null,
        DateTime? occurredAt = null)
    {
        JobId = jobId;
        UserId = userId;
        NavigationEventType = navigationEventType;
        StepNumber = stepNumber;
        TotalSteps = totalSteps;
        Action = action;
        Description = description;
        CurrentUrl = currentUrl;
        TargetElement = targetElement;
        OccurredAt = occurredAt ?? DateTime.UtcNow;
    }
}
