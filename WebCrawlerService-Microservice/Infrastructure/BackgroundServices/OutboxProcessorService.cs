using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebCrawlerService.Domain.Interfaces;
using WebCrawlerService.Infrastructure.Common;

namespace WebCrawlerService.Infrastructure.BackgroundServices;

public class OutboxProcessorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessorService> _logger;
    private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(30);

    public OutboxProcessorService(IServiceProvider serviceProvider, ILogger<OutboxProcessorService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for database migration to complete before starting
        _logger.LogInformation("OutboxProcessorService waiting for database migration to complete...");

        while (!StartupCoordinator.IsMigrationCompleted && !stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }

        if (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("OutboxProcessorService cancelled before migration completed");
            return;
        }

        _logger.LogInformation("✅ Database ready - Outbox processor service starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();

                await outboxService.ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing outbox messages");
            }

            try
            {
                await Task.Delay(_processingInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Outbox processor service stopped");
    }
}