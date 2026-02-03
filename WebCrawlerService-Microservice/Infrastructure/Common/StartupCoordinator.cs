namespace WebCrawlerService.Infrastructure.Common;

/// <summary>
/// Coordinates service startup to ensure database migration completes before background services start
/// </summary>
public static class StartupCoordinator
{
    /// <summary>
    /// Indicates whether database migration has completed successfully
    /// </summary>
    public static bool IsMigrationCompleted { get; set; } = false;
}
