using CrawlData.ApiGateway.Services;
using CrawlData.ApiGateway.Models;
using System.Text;
using System.Text.Json;

namespace CrawlData.ApiGateway.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;
        private readonly IRequestLoggingService _requestLoggingService;

        public RequestLoggingMiddleware(
            RequestDelegate next,
            ILogger<RequestLoggingMiddleware> logger,
            IRequestLoggingService requestLoggingService)
        {
            _next = next;
            _logger = logger;
            _requestLoggingService = requestLoggingService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var startTime = DateTime.UtcNow;
            var correlationId = context.Items["CorrelationId"]?.ToString();
            var requestId = Guid.NewGuid().ToString();

            // Add request ID to headers (in addition to correlation ID)
            context.Response.Headers.Add("X-Request-ID", requestId);

            // Capture response for potential error handling
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                await _requestLoggingService.LogRequestAsync(context, requestId, startTime);
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred. RequestId: {RequestId}, CorrelationId: {CorrelationId}", requestId, correlationId);

                // Create standardized error response
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";

                var errorResponse = ResponseModel.Error("An internal server error occurred.");
                errorResponse.CorrelationId = correlationId;

                //var jsonResponse = JsonSerializer.Serialize(errorResponse);
                //var bytes = Encoding.UTF8.GetBytes(jsonResponse);
                
                //responseBody.SetLength(0);
                //responseBody.Write(bytes, 0, bytes.Length);
            }
            finally
            {
                var endTime = DateTime.UtcNow;
                var duration = endTime - startTime;
                
                await _requestLoggingService.LogResponseAsync(context, requestId, endTime, duration);
                
                _logger.LogInformation(
                    "Request {RequestId} {Method} {Path} responded {StatusCode} in {Duration}ms, CorrelationId: {CorrelationId}",
                    requestId,
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    duration.TotalMilliseconds,
                    correlationId);

                // Copy response back to original stream
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
                context.Response.Body = originalBodyStream;
            }
        }
    }
}