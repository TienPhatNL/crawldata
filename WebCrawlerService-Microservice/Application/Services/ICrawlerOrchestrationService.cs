using WebCrawlerService.Application.Controllers;
using WebCrawlerService.Application.Models;
using WebCrawlerService.Domain.Enums;

namespace WebCrawlerService.Application.Services
{
    public interface ICrawlerOrchestrationService
    {
        Task<Guid> StartCrawlJobAsync(StartCrawlRequest request);
        Task<CrawlJobStatusResponse?> GetJobStatusAsync(Guid jobId, Guid userId);
        Task<bool> CancelJobAsync(Guid jobId, Guid userId);
        Task<IEnumerable<CrawlJobSummary>> GetUserJobsAsync(Guid userId);
        Task ProcessPendingJobsAsync();
        Task<bool> AssignJobToAgentAsync(Guid jobId, Guid agentId);
    }
}