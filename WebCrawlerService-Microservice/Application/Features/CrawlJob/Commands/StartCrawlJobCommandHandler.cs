using MediatR;
using Microsoft.Extensions.Logging;
using WebCrawlerService.Application.Common.Exceptions;
using WebCrawlerService.Application.Common.Interfaces;
using WebCrawlerService.Application.Services;
using WebCrawlerService.Domain.Entities;
using WebCrawlerService.Domain.Enums;
using WebCrawlerService.Domain.Events;
using WebCrawlerService.Domain.Interfaces;

namespace WebCrawlerService.Application.Features.CrawlJob.Commands;

public class StartCrawlJobCommandHandler : IRequestHandler<StartCrawlJobCommand, StartCrawlJobResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainValidationService _domainValidationService;
    private readonly IUserQuotaService _quotaService;
    private readonly ILogger<StartCrawlJobCommandHandler> _logger;

    public StartCrawlJobCommandHandler(
        IUnitOfWork unitOfWork,
        IDomainValidationService domainValidationService,
        IUserQuotaService quotaService,
        ILogger<StartCrawlJobCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _domainValidationService = domainValidationService;
        _quotaService = quotaService;
        _logger = logger;
    }

    public async Task<StartCrawlJobResponse> Handle(StartCrawlJobCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting crawl job for user {UserId} with {UrlCount} URLs", 
            request.UserId, request.Urls.Length);

        // Validate user quota
        var activeJobCount = await _unitOfWork.CrawlJobs.GetActiveJobCountByUserAsync(request.UserId, cancellationToken);
        if (activeJobCount >= 10) // This would come from user's subscription tier
        {
            throw new QuotaExceededException(request.UserId, "active jobs");
        }

        var requiredUnits = request.Urls.Length;
        if (!await _quotaService.CheckQuotaAsync(request.UserId, requiredUnits, cancellationToken))
        {
            throw new QuotaExceededException(request.UserId, "link quota");
        }

        // Validate domain policies for all URLs
        foreach (var url in request.Urls)
        {
            var isAllowed = await _domainValidationService.IsDomainAllowedAsync(url, SubscriptionTier.Free, UserRole.Student);
            if (!isAllowed)
            {
                throw new DomainPolicyViolationException(url);
            }
        }

        // Create crawl job
        var crawlJob = new Domain.Entities.CrawlJob
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            AssignmentId = request.AssignmentId,
            Urls = request.Urls,
            Priority = request.Priority,
            CrawlerType = request.CrawlerType,
            Status = JobStatus.Pending,
            TimeoutSeconds = request.TimeoutSeconds,
            FollowRedirects = request.FollowRedirects,
            ExtractImages = request.ExtractImages,
            ExtractLinks = request.ExtractLinks,
            MaxRetries = request.MaxRetries,
            ConfigurationJson = request.ConfigurationJson,
            CreatedAt = DateTime.UtcNow
        };

        // Add domain event
        crawlJob.AddDomainEvent(new JobStartedEvent(crawlJob));

        // Save to database
        await _unitOfWork.CrawlJobs.AddAsync(crawlJob, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _quotaService.DeductQuotaAsync(request.UserId, requiredUnits, crawlJob.Id, cancellationToken);

        _logger.LogInformation("Crawl job {JobId} created successfully for user {UserId}", 
            crawlJob.Id, request.UserId);

        return new StartCrawlJobResponse
        {
            JobId = crawlJob.Id,
            Status = crawlJob.Status,
            CreatedAt = crawlJob.CreatedAt,
            Message = "Crawl job created successfully and queued for processing"
        };
    }
}
