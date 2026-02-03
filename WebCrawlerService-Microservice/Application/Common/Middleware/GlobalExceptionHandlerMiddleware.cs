using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using WebCrawlerService.Application.Common.Exceptions;

namespace WebCrawlerService.Application.Common.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred while processing the request");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse();

        switch (exception)
        {
            case ValidationException validationException:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "Validation failed";
                response.Details = validationException.Errors.Select(e => e.ErrorMessage).ToList();
                break;

            case CrawlJobNotFoundException:
            case CrawlerAgentNotFoundException:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.Message = exception.Message;
                break;

            case DomainPolicyViolationException:
            case InvalidCrawlConfigurationException:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = exception.Message;
                break;

            case QuotaExceededException:
                response.StatusCode = (int)HttpStatusCode.PaymentRequired;
                response.Message = "Quota exceeded. Please upgrade your plan to continue crawling.";
                response.UpgradeUrl = "/pricing";
                break;

            case CrawlerUnavailableException:
                response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                response.Message = exception.Message;
                break;

            case CrawlJobAlreadyProcessingException:
                response.StatusCode = (int)HttpStatusCode.Conflict;
                response.Message = exception.Message;
                break;

            case UnauthorizedCrawlAccessException:
                response.StatusCode = (int)HttpStatusCode.Forbidden;
                response.Message = exception.Message;
                break;

            case UnauthorizedAccessException:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response.Message = "Unauthorized access";
                break;

            case ArgumentException:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "Invalid request parameters";
                response.Details = new List<string> { exception.Message };
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Message = "An internal server error occurred";
                break;
        }

        context.Response.StatusCode = response.StatusCode;

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}

public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string>? Details { get; set; }
    public string? UpgradeUrl { get; set; }
    public string TraceId { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
