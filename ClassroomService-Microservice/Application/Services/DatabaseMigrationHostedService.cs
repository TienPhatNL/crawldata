using ClassroomService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClassroomService.Application.Services;

/// <summary>
/// Background hosted service that runs database migrations after the application starts.
/// This prevents migrations from blocking the main startup thread and allows the HTTP server to start immediately.
/// </summary>
public class DatabaseMigrationHostedService : IHostedService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<DatabaseMigrationHostedService> _logger;

    public DatabaseMigrationHostedService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<DatabaseMigrationHostedService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("DatabaseMigrationHostedService starting in background...");

        // Run migration in background task to not block startup
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ClassroomDbContext>();

                const int maxRetries = 15; // Increased retries for Docker startup
                var retryCount = 0;

                _logger.LogInformation("Starting database migration...");
                _logger.LogInformation("Waiting for SQL Server Docker container to be ready...");

                // Initial delay to allow SQL Server container to start
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

                while (retryCount < maxRetries)
                {
                    try
                    {
                        _logger.LogInformation("Attempting database migration (attempt {Attempt}/{MaxRetries})",
                            retryCount + 1, maxRetries);

                        // Try to connect and migrate
                        await context.Database.MigrateAsync(cancellationToken);

                        _logger.LogInformation("ClassroomService database migration completed successfully!");
                        return; // Success - exit
                    }
                    catch (Exception ex) when (retryCount < maxRetries - 1)
                    {
                        retryCount++;
                        var exponentialDelay = Math.Pow(2, Math.Min(retryCount, 7)); // Cap at 128 seconds
                        var baseDelay = 5; // Base 5 second delay for Docker startup
                        var totalDelay = TimeSpan.FromSeconds(exponentialDelay + baseDelay);

                        _logger.LogWarning(ex,
                            "Database migration attempt {Attempt} failed. Retrying in {Delay} seconds... (Error: {ErrorMessage})",
                            retryCount, totalDelay.TotalSeconds, ex.Message);

                        await Task.Delay(totalDelay, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Database migration failed after {MaxRetries} attempts. Service will continue but database operations will fail.",
                            maxRetries);
                        _logger.LogError("Make sure SQL Server Docker container is running:");
                        _logger.LogError("  docker ps | grep sql");
                        _logger.LogError("  OR start Aspire which will start SQL Server automatically");
                        return; // Don't throw - allow service to start even if migration fails
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("DatabaseMigrationHostedService was cancelled during startup");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in DatabaseMigrationHostedService");
            }
        }, cancellationToken);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("DatabaseMigrationHostedService stopping...");
        return Task.CompletedTask;
    }
}
