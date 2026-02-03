using MediatR;
using Microsoft.Extensions.Logging;
using WebCrawlerService.Application.Common.Exceptions;
using WebCrawlerService.Application.Common.Interfaces;
using WebCrawlerService.Domain.Interfaces;

namespace WebCrawlerService.Application.Features.CrawlJob.Queries;

public class GetCrawlJobQueryHandler : IRequestHandler<GetCrawlJobQuery, CrawlJobResponse?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetCrawlJobQueryHandler> _logger;

    public GetCrawlJobQueryHandler(IUnitOfWork unitOfWork, ILogger<GetCrawlJobQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CrawlJobResponse?> Handle(GetCrawlJobQuery request, CancellationToken cancellationToken)
    {
        var job = await _unitOfWork.CrawlJobs.GetJobWithResultsAsync(request.JobId, cancellationToken);

        if (job == null)
        {
            _logger.LogWarning("Crawl job {JobId} not found", request.JobId);
            throw new CrawlJobNotFoundException(request.JobId);
        }

        // Authorization check - user can only access their own jobs
        if (request.UserId.HasValue && job.UserId != request.UserId.Value)
        {
            _logger.LogWarning("User {UserId} attempted to access job {JobId} belonging to user {JobOwner}", 
                request.UserId.Value, request.JobId, job.UserId);
            throw new UnauthorizedCrawlAccessException(request.UserId.Value, request.JobId);
        }

        return new CrawlJobResponse
        {
            Id = job.Id,
            UserId = job.UserId,
            AssignmentId = job.AssignmentId,
            Urls = job.Urls,
            Status = job.Status,
            Priority = job.Priority,
            CrawlerType = job.CrawlerType,
            CreatedAt = job.CreatedAt,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt,
            FailedAt = job.FailedAt,
            ErrorMessage = job.ErrorMessage,
            RetryCount = job.RetryCount,
            MaxRetries = job.MaxRetries,
            UrlsProcessed = job.UrlsProcessed,
            UrlsSuccessful = job.UrlsSuccessful,
            UrlsFailed = job.UrlsFailed,
            TotalContentSize = job.TotalContentSize,
            TimeoutSeconds = job.TimeoutSeconds,
            FollowRedirects = job.FollowRedirects,
            ExtractImages = job.ExtractImages,
            ExtractLinks = job.ExtractLinks,
            ConfigurationJson = job.ConfigurationJson,
            AssignedAgent = job.AssignedAgent != null ? new CrawlerAgentResponse
            {
                Id = job.AssignedAgent.Id,
                Name = job.AssignedAgent.Name,
                Type = job.AssignedAgent.Type,
                Status = job.AssignedAgent.Status
            } : null,
            Results = job.Results.Select(r => new CrawlResultResponse
            {
                Id = r.Id,
                Url = r.Url,
                IsSuccess = r.IsSuccess,
                StatusCode = r.StatusCode,
                ContentType = r.ContentType,
                ContentSize = r.ContentSize,
                Title = r.Title,
                Description = r.Description,
                ErrorMessage = r.ErrorMessage,
                CrawledAt = r.CrawledAt,
                Images = r.Images,
                Links = r.Links
            }).ToList()
        };
    }
}