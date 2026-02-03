using CrawlData.ApiGateway.Services;
using System.Net;

namespace CrawlData.ApiGateway.Middleware
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiKeyMiddleware> _logger;
        private readonly IApiKeyValidationService _apiKeyValidationService;

        public ApiKeyMiddleware(
            RequestDelegate next,
            ILogger<ApiKeyMiddleware> logger,
            IApiKeyValidationService apiKeyValidationService)
        {
            _next = next;
            _logger = logger;
            _apiKeyValidationService = apiKeyValidationService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip API key validation for certain paths
            if (ShouldSkipApiKeyValidation(context.Request.Path))
            {
                await _next(context);
                return;
            }

            var apiKey = ExtractApiKey(context);
            
            if (!string.IsNullOrEmpty(apiKey))
            {
                var validationResult = await _apiKeyValidationService.ValidateApiKeyAsync(apiKey);
                
                if (validationResult.IsValid)
                {
                    // Add user information to context
                    context.Items["UserId"] = validationResult.UserId;
                    context.Items["UserRole"] = validationResult.UserRole;
                    context.Items["SubscriptionTier"] = validationResult.SubscriptionTier;
                    
                    _logger.LogDebug("Valid API key for user {UserId}", validationResult.UserId);
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    await context.Response.WriteAsync(new
                    {
                        error = "Invalid API key",
                        message = "The provided API key is invalid or expired."
                    }.ToString());
                    
                    _logger.LogWarning("Invalid API key: {ApiKey}", apiKey.Substring(0, Math.Min(8, apiKey.Length)) + "...");
                    return;
                }
            }

            await _next(context);
        }

        private string? ExtractApiKey(HttpContext context)
        {
            // Try header first
            var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();
            
            // Try query parameter as fallback
            if (string.IsNullOrEmpty(apiKey))
            {
                apiKey = context.Request.Query["api_key"].FirstOrDefault();
            }

            return apiKey;
        }

        private bool ShouldSkipApiKeyValidation(string path)
        {
            var skipPaths = new[]
            {
                "/health",
                "/api/auth/login",
                "/api/auth/register",
                "/api/auth/refresh",
                "/swagger",
                "/docs"
            };

            return skipPaths.Any(skipPath => path.StartsWith(skipPath, StringComparison.OrdinalIgnoreCase));
        }
    }
}