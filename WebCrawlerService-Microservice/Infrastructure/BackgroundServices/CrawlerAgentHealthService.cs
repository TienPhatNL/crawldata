using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebCrawlerService.Domain.Enums;
using WebCrawlerService.Infrastructure.Common;
using WebCrawlerService.Infrastructure.Contexts;

namespace WebCrawlerService.Infrastructure.BackgroundServices;

public class CrawlerAgentHealthService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CrawlerAgentHealthService> _logger;
    private readonly TimeSpan _healthCheckInterval = TimeSpan.FromMinutes(2);
    private readonly TimeSpan _agentTimeout = TimeSpan.FromMinutes(10);

    public CrawlerAgentHealthService(IServiceProvider serviceProvider, ILogger<CrawlerAgentHealthService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for database migration to complete before starting
        _logger.LogInformation("CrawlerAgentHealthService waiting for database migration to complete...");

        while (!StartupCoordinator.IsMigrationCompleted && !stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }

        if (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("CrawlerAgentHealthService cancelled before migration completed");
            return;
        }

        _logger.LogInformation("✅ Database ready - Crawler agent health service starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAgentHealth(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during agent health check");
            }

            try
            {
                await Task.Delay(_healthCheckInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Crawler agent health service stopped");
    }

    private async Task CheckAgentHealth(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrawlerDbContext>();

        var cutoffTime = DateTime.UtcNow.Subtract(_agentTimeout);
        
        // Find agents that haven't reported in for too long
        var unhealthyAgents = await context.CrawlerAgents
            .Where(a => a.Status == AgentStatus.Active)
            .Where(a => a.LastHeartbeat < cutoffTime)
            .ToListAsync(stoppingToken);

        foreach (var agent in unhealthyAgents)
        {
            _logger.LogWarning("Agent {AgentId} ({AgentName}) marked as unhealthy - last heartbeat: {LastHeartbeat}", 
                agent.Id, agent.Name, agent.LastHeartbeat);
            
            agent.Status = AgentStatus.Unhealthy;
            agent.UpdatedAt = DateTime.UtcNow;
            
            // Reassign any active jobs from unhealthy agents
            var activeJobs = await context.CrawlJobs
                .Where(j => j.AssignedAgentId == agent.Id)
                .Where(j => j.Status == JobStatus.InProgress)
                .ToListAsync(stoppingToken);

            foreach (var job in activeJobs)
            {
                _logger.LogInformation("Reassigning job {JobId} from unhealthy agent {AgentId}", job.Id, agent.Id);
                job.AssignedAgentId = null;
                job.Status = JobStatus.Pending;
                job.UpdatedAt = DateTime.UtcNow;
                job.RetryCount++;
            }
        }

        if (unhealthyAgents.Any())
        {
            await context.SaveChangesAsync(stoppingToken);
            _logger.LogInformation("Marked {AgentCount} agents as unhealthy", unhealthyAgents.Count);
        }

        // Clean up old completed jobs (optional cleanup)
        await CleanupOldJobs(context, stoppingToken);
    }

    private async Task CleanupOldJobs(CrawlerDbContext context, CancellationToken stoppingToken)
    {
        var cutoffTime = DateTime.UtcNow.AddDays(-30); // Keep completed jobs for 30 days
        
        var oldCompletedJobs = await context.CrawlJobs
            .Where(j => j.Status == JobStatus.Completed)
            .Where(j => j.CompletedAt < cutoffTime)
            .ToListAsync(stoppingToken);

        if (oldCompletedJobs.Any())
        {
            context.CrawlJobs.RemoveRange(oldCompletedJobs);
            await context.SaveChangesAsync(stoppingToken);
            
            _logger.LogInformation("Cleaned up {JobCount} old completed jobs", oldCompletedJobs.Count);
        }
    }
}