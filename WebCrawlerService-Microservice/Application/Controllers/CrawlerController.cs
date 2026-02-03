using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebCrawlerService.Application.Common.Models;
using WebCrawlerService.Application.Common.Security;
using WebCrawlerService.Application.Features.CrawlJob.Commands;
using WebCrawlerService.Application.Features.CrawlJob.Queries;
using WebCrawlerService.Application.Services.Crawl4AI;
using WebCrawlerService.Application.Services;
using WebCrawlerService.Domain.Entities;
using WebCrawlerService.Domain.Enums;

namespace WebCrawlerService.Application.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = Policies.RequireAuthentication)]
    public class CrawlerController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IDomainValidationService _domainValidationService;
        private readonly ISmartCrawlerOrchestrationService _orchestrationService;

        public CrawlerController(
            IMediator mediator,
            IDomainValidationService domainValidationService,
            ISmartCrawlerOrchestrationService orchestrationService)
        {
            _mediator = mediator;
            _domainValidationService = domainValidationService;
            _orchestrationService = orchestrationService;
        }

        [HttpPost("ask")]
        [AllowAnonymous]
        public async Task<ActionResult<object>> AskQuestion([FromBody] AskQuestionRequest request)
        {
            var answer = await _orchestrationService.ProcessUserMessageAsync(
                request.ConversationId, 
                request.Question,
                request.CsvContext);
            return Ok(new { Answer = answer });
        }

        [HttpGet("ask")]
        [AllowAnonymous]
        public ActionResult<object> AskQuestionGet()
        {
            return BadRequest("Please use POST method with JSON body: { \"conversationId\": \"...\", \"question\": \"...\" }");
        }

        [HttpPost("start")]
        public async Task<ActionResult<StartCrawlJobResponse>> StartCrawlJob([FromBody] StartCrawlRequest request)
        {
            // Validate URLs against domain policies
            var validationResult = await _domainValidationService.ValidateUrlsAsync(
                request.Urls, request.UserId, request.UserTier);

            if (!validationResult.AllUrlsValid)
            {
                return BadRequest(new { 
                    Message = "Some URLs are not allowed", 
                    InvalidUrls = validationResult.InvalidUrls 
                });
            }

            var command = new StartCrawlJobCommand
            {
                UserId = request.UserId,
                Urls = request.Urls,
                Priority = request.Priority,
                CrawlerType = request.CrawlerType,
                AssignmentId = request.AssignmentId,
                TimeoutSeconds = request.Configuration?.TimeoutSeconds ?? 30,
                FollowRedirects = request.Configuration?.FollowRedirects ?? true,
                ExtractImages = request.Configuration?.ExtractImages ?? false,
                ExtractLinks = request.Configuration?.ExtractLinks ?? true,
                ConfigurationJson = request.Configuration?.CustomConfigJson
            };

            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpGet("{jobId}/status")]
        [AllowAnonymous] // Allow service-to-service calls from ClassroomService
        public async Task<ActionResult<CrawlJobResponse>> GetJobStatus(Guid jobId, [FromQuery] Guid userId)
        {
            var query = new GetCrawlJobQuery
            {
                JobId = jobId,
                UserId = userId
            };

            var response = await _mediator.Send(query);
            return Ok(response);
        }

        [HttpPost("{jobId}/cancel")]
        public async Task<ActionResult<CancelCrawlJobResponse>> CancelJob(Guid jobId, [FromQuery] Guid userId)
        {
            var command = new CancelCrawlJobCommand
            {
                JobId = jobId,
                UserId = userId
            };

            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpGet("user/{userId}/jobs")]
        [Authorize(Policy = Policies.CanAccessUserData)]
        public async Task<ActionResult<PagedResult<CrawlJobSummaryResponse>>> GetUserJobs(
            Guid userId, 
            [FromQuery] JobStatus? status = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = new GetUserJobsQuery
            {
                UserId = userId,
                Status = status,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var response = await _mediator.Send(query);
            return Ok(response);
        }

        [HttpPost("validate-urls")]
        public async Task<ActionResult<UrlValidationResponse>> ValidateUrls([FromBody] UrlValidationRequest request)
        {
            var result = await _domainValidationService.ValidateUrlsAsync(
                request.Urls, request.UserId, request.UserTier);
            
            return Ok(result);
        }
    }

    // Request/Response models
    public class StartCrawlRequest
    {
        public Guid UserId { get; set; }
        public string[] Urls { get; set; } = Array.Empty<string>();
        public CrawlerType CrawlerType { get; set; } = CrawlerType.HttpClient;
        public Priority Priority { get; set; } = Priority.Normal;
        public Guid? AssignmentId { get; set; }
        public CrawlerConfiguration? Configuration { get; set; }
        public SubscriptionTier UserTier { get; set; } = SubscriptionTier.Free;
    }

    public class CrawlerConfiguration
    {
        public int TimeoutSeconds { get; set; } = 30;
        public bool FollowRedirects { get; set; } = true;
        public bool ExtractImages { get; set; } = false;
        public bool ExtractLinks { get; set; } = true;
        public string? CustomConfigJson { get; set; }
    }


    public class UrlValidationRequest
    {
        public string[] Urls { get; set; } = Array.Empty<string>();
        public Guid UserId { get; set; }
        public SubscriptionTier UserTier { get; set; }
    }

    public class UrlValidationResponse
    {
        public bool AllUrlsValid { get; set; }
        public UrlValidationResult[] Results { get; set; } = Array.Empty<UrlValidationResult>();
        public string[] InvalidUrls { get; set; } = Array.Empty<string>();
    }

    public class AskQuestionRequest
    {
        public Guid ConversationId { get; set; }
        public string Question { get; set; } = string.Empty;
        public string? CsvContext { get; set; } // Optional CSV data context
    }

    public class UrlValidationResult
    {
        public string Url { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public string? ValidationError { get; set; }
        public bool IsAllowedDomain { get; set; }
        public string? DomainRestrictionReason { get; set; }
    }
}