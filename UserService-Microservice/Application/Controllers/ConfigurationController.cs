using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserService.Application.Features.Configuration.Commands;
using UserService.Application.Features.Configuration.Queries;

namespace UserService.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class ConfigurationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ConfigurationController> _logger;

    public ConfigurationController(IMediator mediator, ILogger<ConfigurationController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all allowed email domains (Admin only)
    /// </summary>
    [HttpGet("allowed-email-domains")]
    [ProducesResponseType(typeof(List<AllowedEmailDomainDto>), 200)]
    public async Task<IActionResult> GetAllowedEmailDomains([FromQuery] bool onlyActive = true)
    {
        var query = new GetAllowedEmailDomainsQuery { OnlyActive = onlyActive };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Add a new allowed email domain (Admin only)
    /// </summary>
    [HttpPost("allowed-email-domains")]
    [ProducesResponseType(typeof(AddAllowedEmailDomainResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> AddAllowedEmailDomain([FromBody] AddAllowedEmailDomainCommand command)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var requestedBy))
        {
            return Unauthorized(new { message = "Invalid user token" });
        }

        command.RequestedBy = requestedBy;
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Toggle email domain active status (Admin only)
    /// </summary>
    [HttpPatch("allowed-email-domains/{id}/toggle-status")]
    [ProducesResponseType(typeof(ToggleEmailDomainStatusResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ToggleEmailDomainStatus(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var requestedBy))
        {
            return Unauthorized(new { message = "Invalid user token" });
        }

        var command = new ToggleEmailDomainStatusCommand
        {
            DomainId = id,
            RequestedBy = requestedBy
        };

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Delete an allowed email domain (Admin only)
    /// </summary>
    [HttpDelete("allowed-email-domains/{id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteAllowedEmailDomain(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var requestedBy))
        {
            return Unauthorized(new { message = "Invalid user token" });
        }

        // For now, we'll just deactivate instead of hard delete
        var command = new ToggleEmailDomainStatusCommand
        {
            DomainId = id,
            RequestedBy = requestedBy
        };

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return NotFound(new { message = "Email domain not found" });
        }

        return Ok(new { message = $"Email domain deactivated successfully", isActive = result.NewStatus });
    }
}
