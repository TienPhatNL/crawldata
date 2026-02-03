using System.Linq.Expressions;
using MediatR;
using WebCrawlerService.Application.Common.Interfaces;
using WebCrawlerService.Domain.Interfaces;
using WebCrawlerService.Domain.Common;

namespace WebCrawlerService.Application.Features.CrawlJob.Queries;

public class GetUserJobsQueryHandler : IRequestHandler<GetUserJobsQuery, PagedResult<CrawlJobSummaryResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserJobsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<CrawlJobSummaryResponse>> Handle(GetUserJobsQuery request, CancellationToken cancellationToken)
    {
        Expression<Func<Domain.Entities.CrawlJob, bool>> filter = request.Status.HasValue
            ? job => job.UserId == request.UserId && job.Status == request.Status.Value
            : job => job.UserId == request.UserId;

        var jobs = await _unitOfWork.CrawlJobs.GetPagedAsync(
            filter: filter,
            orderBy: q => q.OrderByDescending(j => j.CreatedAt),
            pageNumber: request.PageNumber,
            pageSize: request.PageSize,
            cancellationToken: cancellationToken);

        var summaries = jobs.Data.Select(job => new CrawlJobSummaryResponse
        {
            Id = job.Id,
            AssignmentId = job.AssignmentId,
            UrlCount = job.Urls.Length,
            Status = job.Status,
            Priority = job.Priority,
            CrawlerType = job.CrawlerType,
            CreatedAt = job.CreatedAt,
            CompletedAt = job.CompletedAt,
            UrlsProcessed = job.UrlsProcessed,
            UrlsSuccessful = job.UrlsSuccessful,
            UrlsFailed = job.UrlsFailed,
            TotalContentSize = job.TotalContentSize,
            ErrorMessage = job.ErrorMessage
        });

        return PagedResult<CrawlJobSummaryResponse>.Create(
            summaries, jobs.TotalCount, jobs.PageNumber, jobs.PageSize);
    }
}