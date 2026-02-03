using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserService.Application.Common.Models;
using UserService.Application.Features.ApiKeys.Commands;
using UserService.Application.Features.ApiKeys.Queries;

namespace UserService.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "PaidUser")] // Only Paid Users (Premium/Enterprise) can manage API keys
public class ApiKeyController : ControllerBase
{
    private readonly IMediator _mediator;

    public ApiKeyController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all API keys for current user (Premium/Enterprise only)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ResponseModel>> GetApiKeys()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Invalid user token" });
        }

        var query = new GetApiKeysQuery { UserId = userId };
        var response = await _mediator.Send(query);
        return StatusCode((int)response.Status!, response);
    }

    /// <summary>
    /// Create a new API key (Premium/Enterprise only)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ResponseModel>> CreateApiKey([FromBody] CreateApiKeyCommand command)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Invalid user token" });
        }

        command.UserId = userId;
        var response = await _mediator.Send(command);
        return StatusCode((int)response.Status!, response);
    }

    /// <summary>
    /// Revoke/deactivate an API key (Premium/Enterprise only)
    /// </summary>
    [HttpDelete("{apiKeyId:guid}")]
    public async Task<ActionResult<ResponseModel>> RevokeApiKey(Guid apiKeyId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Invalid user token" });
        }

        var command = new RevokeApiKeyCommand
        {
            UserId = userId,
            ApiKeyId = apiKeyId
        };

        var response = await _mediator.Send(command);
        return StatusCode((int)response.Status!, response);
    }

    /// <summary>
    /// Get API key usage statistics (Premium/Enterprise only)
    /// </summary>
    [HttpGet("{apiKeyId:guid}/usage")]
    public async Task<ActionResult<object>> GetApiKeyUsage(Guid apiKeyId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Invalid user token" });
        }

        // TODO: Implement GetApiKeyUsageQuery
        return Ok(new 
        { 
            message = "API key usage statistics endpoint - to be implemented",
            apiKeyId = apiKeyId,
            userId = userId 
        });
    }

    /// <summary>
    /// Update API key settings (name, description, scopes) - Premium/Enterprise only
    /// </summary>
    [HttpPut("{apiKeyId:guid}")]
    public async Task<ActionResult<object>> UpdateApiKey(Guid apiKeyId, [FromBody] UpdateApiKeyRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Invalid user token" });
        }

        // TODO: Implement UpdateApiKeyCommand
        return Ok(new 
        { 
            message = "API key update endpoint - to be implemented",
            apiKeyId = apiKeyId,
            userId = userId,
            request = request
        });
    }

    /// <summary>
    /// Get available API scopes and their descriptions
    /// </summary>
    [HttpGet("scopes")]
    public ActionResult<object> GetAvailableScopes()
    {
        var scopes = new[]
        {
            new { Scope = "crawl:basic", Description = "Basic web crawling capabilities" },
            new { Scope = "crawl:advanced", Description = "Advanced crawling with custom configurations" },
            new { Scope = "data:read", Description = "Read access to crawled data" },
            new { Scope = "data:write", Description = "Write access to manage crawled data" },
            new { Scope = "analytics:read", Description = "Access to analytics and reports" },
            new { Scope = "analytics:write", Description = "Create and manage custom analytics" },
            new { Scope = "user:profile", Description = "Access to user profile information" },
            new { Scope = "quota:read", Description = "View quota usage information" }
        };

        return Ok(new { scopes });
    }
}

// Request DTOs
public class UpdateApiKeyRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<string>? Scopes { get; set; }
}