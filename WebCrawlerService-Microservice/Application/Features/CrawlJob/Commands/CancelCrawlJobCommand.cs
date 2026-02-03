using MediatR;

namespace WebCrawlerService.Application.Features.CrawlJob.Commands;

public class CancelCrawlJobCommand : IRequest<CancelCrawlJobResponse>
{
    public Guid JobId { get; set; }
    public Guid UserId { get; set; } // For authorization
}

public class CancelCrawlJobResponse
{
    public Guid JobId { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}