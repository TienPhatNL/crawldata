using Microsoft.AspNetCore.Mvc;
using HttpStatusCodes = Microsoft.AspNetCore.Http.StatusCodes;

namespace ClassroomService.Application.Controllers;

/// <summary>
/// Controller for health checks and service status
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Health")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Gets the health status of the service
    /// </summary>
    /// <returns>Health status information</returns>
    /// <response code="200">Service is healthy</response>
    [HttpGet]
    [ProducesResponseType(typeof(object), HttpStatusCodes.Status200OK)]
    public ActionResult GetHealth()
    {
        return Ok(new
        {
            Status = "Healthy",
            Service = "ClassroomService",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        });
    }

    /// <summary>
    /// Gets detailed service information
    /// </summary>
    /// <returns>Service information</returns>
    /// <response code="200">Service information retrieved</response>
    [HttpGet("info")]
    [ProducesResponseType(typeof(object), HttpStatusCodes.Status200OK)]
    public ActionResult GetInfo()
    {
        return Ok(new
        {
            ServiceName = "ClassroomService",
            Version = "1.0.0",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            MachineName = Environment.MachineName,
            ProcessId = Environment.ProcessId,
            WorkingSet = Environment.WorkingSet,
            Timestamp = DateTime.UtcNow
        });
    }
}