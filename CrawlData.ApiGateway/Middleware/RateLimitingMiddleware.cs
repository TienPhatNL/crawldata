using CrawlData.ApiGateway.Services;
using CrawlData.ApiGateway.Models;
using System.Net;
using System.Text.Json;

namespace CrawlData.ApiGateway.Middleware
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private readonly IRateLimitingService _rateLimitingService;

        public RateLimitingMiddleware(
            RequestDelegate next,
            ILogger<RateLimitingMiddleware> logger,
            IRateLimitingService rateLimitingService)
        {
            _next = next;
            _logger = logger;
            _rateLimitingService = rateLimitingService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientId = GetClientId(context);
            var isAllowed = await _rateLimitingService.IsRequestAllowedAsync(clientId, context);

            if (!isAllowed.IsAllowed)
            {
                var correlationId = context.Items["CorrelationId"]?.ToString();
                
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.ContentType = "application/json";
                context.Response.Headers.Add("Retry-After", isAllowed.RetryAfterSeconds.ToString());

                var rateLimitResponse = ResponseModel.TooManyRequests("Too many requests. Please try again later.");
                rateLimitResponse.CorrelationId = correlationId;
                rateLimitResponse.Data = new
                {
                    retryAfter = isAllowed.RetryAfterSeconds,
                    limit = isAllowed.Limit,
                    remaining = 0
                };

                //var jsonResponse = JsonSerializer.Serialize(rateLimitResponse);
                //await context.Response.WriteAsync(jsonResponse);

                _logger.LogWarning("Rate limit exceeded for client {ClientId}, CorrelationId: {CorrelationId}", clientId, correlationId);
                return;
            }

            // Add rate limit headers
            context.Response.Headers.Add("X-RateLimit-Limit", isAllowed.Limit.ToString());
            context.Response.Headers.Add("X-RateLimit-Remaining", isAllowed.Remaining.ToString());
            context.Response.Headers.Add("X-RateLimit-Reset", isAllowed.ResetTime.ToString());

            await _next(context);
        }

        private string GetClientId(HttpContext context)
        {
            // Try to get user ID from JWT token
            var userId = context.User?.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                return $"user:{userId}";
            }

            // Try to get API key
            var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();
            if (!string.IsNullOrEmpty(apiKey))
            {
                return $"apikey:{apiKey}";
            }

            // Fall back to IP address
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return $"ip:{ipAddress}";
        }
    }
}