using MediatR;
using Microsoft.Extensions.Logging;
using WebCrawlerService.Application.Common.Exceptions;
using WebCrawlerService.Application.Common.Interfaces;
using WebCrawlerService.Domain.Interfaces;
using WebCrawlerService.Domain.Enums;

namespace WebCrawlerService.Application.Features.CrawlJob.Commands;

public class CancelCrawlJobCommandHandler : IRequestHandler<CancelCrawlJobCommand, CancelCrawlJobResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CancelCrawlJobCommandHandler> _logger;

    public CancelCrawlJobCommandHandler(IUnitOfWork unitOfWork, ILogger<CancelCrawlJobCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CancelCrawlJobResponse> Handle(CancelCrawlJobCommand request, CancellationToken cancellationToken)
    {
        var job = await _unitOfWork.CrawlJobs.GetByIdAsync(request.JobId, cancellationToken);

        if (job == null)
        {
            throw new CrawlJobNotFoundException(request.JobId);
        }

        // Authorization check
        if (job.UserId != request.UserId)
        {
            throw new UnauthorizedCrawlAccessException(request.UserId, request.JobId);
        }

        // Check if job can be canceled
        if (job.Status == JobStatus.Completed || job.Status == JobStatus.Failed || job.Status == JobStatus.Cancelled)
        {
            return new CancelCrawlJobResponse
            {
                JobId = request.JobId,
                Success = false,
                Message = $"Job cannot be cancelled because it is already {job.Status}"
            };
        }

        // Cancel the job
        job.Status = JobStatus.Cancelled;
        job.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.CrawlJobs.Update(job);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Crawl job {JobId} cancelled by user {UserId}", request.JobId, request.UserId);

        return new CancelCrawlJobResponse
        {
            JobId = request.JobId,
            Success = true,
            Message = "Job cancelled successfully"
        };
    }
}