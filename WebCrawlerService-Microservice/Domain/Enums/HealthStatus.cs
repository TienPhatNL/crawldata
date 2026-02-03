namespace WebCrawlerService.Domain.Enums
{
    public enum HealthStatus
    {
        Healthy = 0,
        Degraded = 1,
        Unhealthy = 2,
        Initializing = 3,
        ShuttingDown = 4
    }
}
