using Microsoft.Extensions.Logging;
using WebCrawlerService.Domain.Entities;
using WebCrawlerService.Domain.Enums;
using WebCrawlerService.Domain.Interfaces;
using WebCrawlerService.Infrastructure.Agents;

namespace WebCrawlerService.Infrastructure.Services;

/// <summary>
/// Factory for selecting and creating appropriate crawler agent based on job requirements
/// Implements strategy pattern for crawler selection
/// </summary>
public class CrawlerAgentFactory
{
    private readonly ShopeeCrawlerAgent _shopeeAgent;
    private readonly MobileMcpCrawlerAgent _mobileMcpAgent;
    private readonly ICrawlTemplateRepository _templateRepository;
    private readonly ILogger<CrawlerAgentFactory> _logger;

    public CrawlerAgentFactory(
        ShopeeCrawlerAgent shopeeAgent,
        MobileMcpCrawlerAgent mobileMcpAgent,
        ICrawlTemplateRepository templateRepository,
        ILogger<CrawlerAgentFactory> logger)
    {
        _shopeeAgent = shopeeAgent;
        _mobileMcpAgent = mobileMcpAgent;
        _templateRepository = templateRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get the appropriate crawler agent for a given job
    /// </summary>
    public async Task<ICrawlerAgentExecutor> GetAgentAsync(
        CrawlJob job,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Selecting crawler agent for job {JobId} with type {CrawlerType}",
            job.Id, job.CrawlerType);

        // Explicit crawler type selection
        if (job.CrawlerType == CrawlerType.MobileMcp)
        {
            _logger.LogInformation("Selected MobileMcpCrawlerAgent for job {JobId}", job.Id);
            return new MobileMcpCrawlerAgentExecutor(_mobileMcpAgent);
        }

        if (job.CrawlerType == CrawlerType.AppSpecificApi)
        {
            if (await _shopeeAgent.CanHandleAsync(job, cancellationToken))
            {
                _logger.LogInformation("Selected ShopeeCrawlerAgent for job {JobId}", job.Id);
                return new ShopeeCrawlerAgentExecutor(_shopeeAgent);
            }
        }

        // Template-based selection
        if (job.TemplateId.HasValue)
        {
            var template = await _templateRepository.GetByIdAsync(job.TemplateId.Value, cancellationToken);
            if (template != null)
            {
                if (template.MobileApiProvider == MobileApiProvider.Shopee)
                {
                    _logger.LogInformation(
                        "Selected ShopeeCrawlerAgent for job {JobId} based on template {TemplateId}",
                        job.Id, template.Id);
                    return new ShopeeCrawlerAgentExecutor(_shopeeAgent);
                }
            }
        }

        // URL-based auto-detection
        if (job.CrawlerType == CrawlerType.Universal || job.CrawlerType == CrawlerType.Playwright)
        {
            if (await _shopeeAgent.CanHandleAsync(job, cancellationToken))
            {
                _logger.LogInformation(
                    "Auto-detected Shopee URL for job {JobId}, using ShopeeCrawlerAgent",
                    job.Id);
                return new ShopeeCrawlerAgentExecutor(_shopeeAgent);
            }
        }

        // Default: return null to indicate no specialized agent available
        // The job processor should fall back to standard HTTP/Playwright agents
        _logger.LogWarning(
            "No specialized agent available for job {JobId} with type {CrawlerType}",
            job.Id, job.CrawlerType);

        return null;
    }

    /// <summary>
    /// Get agent by explicit crawler type
    /// </summary>
    public ICrawlerAgentExecutor? GetAgentByType(CrawlerType type)
    {
        return type switch
        {
            CrawlerType.MobileMcp => new MobileMcpCrawlerAgentExecutor(_mobileMcpAgent),
            CrawlerType.AppSpecificApi => new ShopeeCrawlerAgentExecutor(_shopeeAgent),
            _ => null
        };
    }
}

/// <summary>
/// Interface for crawler agent execution
/// Allows polymorphic handling of different crawler types
/// </summary>
public interface ICrawlerAgentExecutor
{
    Task<List<CrawlResult>> ExecuteAsync(CrawlJob job, CancellationToken cancellationToken);
    string AgentName { get; }
    CrawlerType CrawlerType { get; }
}

/// <summary>
/// Executor wrapper for MobileMcpCrawlerAgent
/// </summary>
public class MobileMcpCrawlerAgentExecutor : ICrawlerAgentExecutor
{
    private readonly MobileMcpCrawlerAgent _agent;

    public MobileMcpCrawlerAgentExecutor(MobileMcpCrawlerAgent agent)
    {
        _agent = agent;
    }

    public async Task<List<CrawlResult>> ExecuteAsync(
        CrawlJob job,
        CancellationToken cancellationToken)
    {
        return await _agent.ExecuteAsync(job, cancellationToken);
    }

    public string AgentName => "MobileMcpCrawlerAgent";
    public CrawlerType CrawlerType => CrawlerType.MobileMcp;
}

/// <summary>
/// Executor wrapper for ShopeeCrawlerAgent
/// </summary>
public class ShopeeCrawlerAgentExecutor : ICrawlerAgentExecutor
{
    private readonly ShopeeCrawlerAgent _agent;

    public ShopeeCrawlerAgentExecutor(ShopeeCrawlerAgent agent)
    {
        _agent = agent;
    }

    public async Task<List<CrawlResult>> ExecuteAsync(
        CrawlJob job,
        CancellationToken cancellationToken)
    {
        return await _agent.ExecuteAsync(job, cancellationToken);
    }

    public string AgentName => "ShopeeCrawlerAgent";
    public CrawlerType CrawlerType => CrawlerType.AppSpecificApi;
}
