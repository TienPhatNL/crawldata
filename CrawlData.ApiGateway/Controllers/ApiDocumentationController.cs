using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrawlData.ApiGateway.Controllers;

/// <summary>
/// API Documentation Controller - Documents all routes available through the API Gateway
/// </summary>
[ApiController]
[Route("api/gateway")]
[Produces("application/json")]
public class ApiDocumentationController : ControllerBase
{
    /// <summary>
    /// Get API Gateway information and available routes
    /// </summary>
    /// <returns>Information about the API Gateway and its routes</returns>
    [HttpGet("info")]
    [AllowAnonymous]
    public ActionResult<GatewayInfo> GetGatewayInfo()
    {
        var gatewayInfo = new GatewayInfo
        {
            Name = "CrawlData API Gateway",
            Version = "1.0.0",
            Description = "API Gateway for CrawlData Platform - A comprehensive web crawling and analysis platform",
            AvailableRoutes = new List<RouteInfo>
            {
                new RouteInfo
                {
                    Path = "/api/auth",
                    Service = "UserService",
                    Description = "Authentication endpoints",
                    Methods = new[] { "POST" },
                    Endpoints = new[]
                    {
                        "/api/auth/register - Register new user (Lecturer/PaidUser)",
                        "/api/auth/login - User login with JWT",
                        "/api/auth/confirm-email - Confirm email address",
                        "/api/auth/forgot-password - Request password reset",
                        "/api/auth/reset-password - Reset password with token",
                        "/api/auth/refresh-token - Refresh JWT token"
                    }
                },
                new RouteInfo
                {
                    Path = "/api/user",
                    Service = "UserService",
                    Description = "User management endpoints",
                    Methods = new[] { "GET", "PUT", "DELETE" },
                    Endpoints = new[]
                    {
                        "/api/user/profile - Get/Update user profile",
                        "/api/user/change-password - Change password",
                        "/api/user/delete-account - Delete user account",
                        "/api/user/preferences - User preferences"
                    }
                },
                new RouteInfo
                {
                    Path = "/api/admin",
                    Service = "UserService",
                    Description = "Admin management endpoints",
                    Methods = new[] { "GET", "POST", "PUT", "DELETE" },
                    RequiredRole = "Admin",
                    Endpoints = new[]
                    {
                        "/api/admin/users - Manage users",
                        "/api/admin/analytics - System analytics",
                        "/api/admin/system-health - System health monitoring",
                        "/api/admin/audit-logs - Audit log management"
                    }
                },
                new RouteInfo
                {
                    Path = "/api/subscription",
                    Service = "UserService",
                    Description = "Subscription management (User Service)",
                    Methods = new[] { "GET", "POST", "PUT" },
                    Endpoints = new[]
                    {
                        "/api/subscription/current - Get current subscription",
                        "/api/subscription/upgrade - Upgrade subscription",
                        "/api/subscription/cancel - Cancel subscription"
                    }
                },
                new RouteInfo
                {
                    Path = "/api/subscriptions",
                    Service = "SubscriptionService",
                    Description = "Advanced subscription management",
                    Methods = new[] { "GET", "POST", "PUT", "DELETE" },
                    Endpoints = new[]
                    {
                        "/api/subscriptions/plans - Available subscription plans",
                        "/api/subscriptions/billing - Billing management",
                        "/api/subscriptions/usage - Usage tracking",
                        "/api/subscriptions/invoices - Invoice management"
                    }
                },
                new RouteInfo
                {
                    Path = "/api/apikey",
                    Service = "UserService",
                    Description = "API key management",
                    Methods = new[] { "GET", "POST", "PUT", "DELETE" },
                    RequiredRole = "PaidUser",
                    Endpoints = new[]
                    {
                        "/api/apikey/generate - Generate new API key",
                        "/api/apikey/list - List user API keys",
                        "/api/apikey/revoke - Revoke API key",
                        "/api/apikey/usage - API key usage statistics"
                    }
                },
                new RouteInfo
                {
                    Path = "/api/webcrawler",
                    Service = "WebCrawlerService",
                    Description = "Web crawling operations",
                    Methods = new[] { "GET", "POST", "PUT", "DELETE" },
                    Endpoints = new[]
                    {
                        "/api/webcrawler/crawl - Start crawling job",
                        "/api/webcrawler/jobs - Manage crawling jobs",
                        "/api/webcrawler/status - Get crawl status",
                        "/api/webcrawler/results - Get crawl results",
                        "/api/webcrawler/agents - Manage crawling agents (HTTP, Selenium, Playwright)"
                    }
                },
                new RouteInfo
                {
                    Path = "/api/dataextraction",
                    Service = "DataExtractionService",
                    Description = "AI-powered content analysis",
                    Methods = new[] { "GET", "POST", "PUT", "DELETE" },
                    Endpoints = new[]
                    {
                        "/api/dataextraction/analyze - Analyze crawled content",
                        "/api/dataextraction/jobs - Manage analysis jobs",
                        "/api/dataextraction/providers - AI provider management (OpenAI, Claude)",
                        "/api/dataextraction/results - Get analysis results"
                    }
                },
                new RouteInfo
                {
                    Path = "/api/reports",
                    Service = "ReportGenerationService",
                    Description = "Report generation and export",
                    Methods = new[] { "GET", "POST", "PUT", "DELETE" },
                    Endpoints = new[]
                    {
                        "/api/reports/generate - Generate reports",
                        "/api/reports/templates - Manage report templates",
                        "/api/reports/export - Export reports (PDF, Excel, HTML, JSON, CSV)",
                        "/api/reports/download - Download generated reports"
                    }
                }
            },
            Authentication = new AuthenticationInfo
            {
                JwtBearer = "Required for most endpoints. Use 'Bearer {token}' in Authorization header",
                ApiKey = "Alternative authentication for programmatic access. Use 'X-API-Key' header",
                Roles = new[] { "Admin", "Staff", "Lecturer", "Student", "PaidUser" }
            },
            RateLimiting = new RateLimitingInfo
            {
                DefaultLimits = "100 requests/minute, 1000 requests/hour",
                PaidUserLimits = "500 requests/minute, 10000 requests/hour"
            }
        };

        return Ok(gatewayInfo);
    }

    /// <summary>
    /// Get health status of all services behind the gateway
    /// </summary>
    /// <returns>Health status of all microservices</returns>
    [HttpGet("health")]
    [AllowAnonymous]
    public ActionResult<ServiceHealthStatus> GetServicesHealth()
    {
        var healthStatus = new ServiceHealthStatus
        {
            Gateway = "Healthy",
            Services = new Dictionary<string, string>
            {
                { "UserService", "Check /api/user/health" },
                { "WebCrawlerService", "Check /api/webcrawler/health" },
                { "DataExtractionService", "Check /api/dataextraction/health" },
                { "SubscriptionService", "Check /api/subscriptions/health" },
                { "ReportGenerationService", "Check /api/reports/health" }
            },
            Timestamp = DateTime.UtcNow
        };

        return Ok(healthStatus);
    }
}

/// <summary>
/// Gateway information model
/// </summary>
public class GatewayInfo
{
    /// <summary>
    /// Gateway name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gateway version
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gateway description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Available routes through the gateway
    /// </summary>
    public List<RouteInfo> AvailableRoutes { get; set; } = new();

    /// <summary>
    /// Authentication information
    /// </summary>
    public AuthenticationInfo Authentication { get; set; } = new();

    /// <summary>
    /// Rate limiting information
    /// </summary>
    public RateLimitingInfo RateLimiting { get; set; } = new();
}

/// <summary>
/// Route information model
/// </summary>
public class RouteInfo
{
    /// <summary>
    /// Route path pattern
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Target microservice
    /// </summary>
    public string Service { get; set; } = string.Empty;

    /// <summary>
    /// Route description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Supported HTTP methods
    /// </summary>
    public string[] Methods { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Required role for access (if any)
    /// </summary>
    public string? RequiredRole { get; set; }

    /// <summary>
    /// Available endpoints under this route
    /// </summary>
    public string[] Endpoints { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Authentication information model
/// </summary>
public class AuthenticationInfo
{
    /// <summary>
    /// JWT Bearer token information
    /// </summary>
    public string JwtBearer { get; set; } = string.Empty;

    /// <summary>
    /// API Key authentication information
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Available user roles
    /// </summary>
    public string[] Roles { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Rate limiting information model
/// </summary>
public class RateLimitingInfo
{
    /// <summary>
    /// Default rate limits for standard users
    /// </summary>
    public string DefaultLimits { get; set; } = string.Empty;

    /// <summary>
    /// Rate limits for paid users
    /// </summary>
    public string PaidUserLimits { get; set; } = string.Empty;
}

/// <summary>
/// Service health status model
/// </summary>
public class ServiceHealthStatus
{
    /// <summary>
    /// Gateway health status
    /// </summary>
    public string Gateway { get; set; } = string.Empty;

    /// <summary>
    /// Individual service health status
    /// </summary>
    public Dictionary<string, string> Services { get; set; } = new();

    /// <summary>
    /// Status check timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }
}