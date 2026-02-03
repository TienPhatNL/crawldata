using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrawlData.ApiGateway.Controllers;

/// <summary>
/// Health Check Controller for API Gateway monitoring
/// </summary>
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get basic health status of the API Gateway
    /// </summary>
    /// <returns>Basic health information</returns>
    /// <response code="200">Gateway is healthy</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            service = "API Gateway"
        });
    }

    /// <summary>
    /// Get detailed health status including dependencies
    /// </summary>
    /// <returns>Detailed health information including downstream services</returns>
    /// <response code="200">Detailed health status retrieved successfully</response>
    [HttpGet("detailed")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetDetailed()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            service = "API Gateway",
            uptime = TimeSpan.FromTicks(Environment.TickCount64),
            dependencies = new
            {
                redis = "healthy", // TODO: Add actual health checks
                webcrawler = "healthy",
                dataextraction = "healthy",
                subscription = "healthy",
                reportgeneration = "healthy"
            }
        });
    }
}