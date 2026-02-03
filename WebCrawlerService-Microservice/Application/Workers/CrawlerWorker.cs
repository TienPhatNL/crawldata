namespace WebCrawlerService.Application.Workers
{
    public class CrawlerWorker : BackgroundService
    {
        private readonly ILogger<CrawlerWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public CrawlerWorker(
            ILogger<CrawlerWorker> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CrawlerWorker starting");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    
                    // Process pending crawl jobs
                    await ProcessCrawlJobsAsync(scope, stoppingToken);
                    
                    // Wait before next iteration
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in CrawlerWorker");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }

            _logger.LogInformation("CrawlerWorker stopping");
        }

        private async Task ProcessCrawlJobsAsync(IServiceScope scope, CancellationToken cancellationToken)
        {
            // Placeholder implementation
            _logger.LogDebug("Processing crawl jobs");
            await Task.CompletedTask;
        }
    }
}