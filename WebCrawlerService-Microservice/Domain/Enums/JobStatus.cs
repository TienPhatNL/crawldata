namespace WebCrawlerService.Domain.Enums
{
    public enum JobStatus
    {
        Pending = 0,
        Queued = 1,
        Assigned = 2,
        InProgress = 3,
        Running = 4,
        Completed = 5,
        Failed = 6,
        Cancelled = 7,
        Paused = 8
    }
}