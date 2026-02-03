using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.Common.Models;
using UserService.Application.Features.SubscriptionTiers.Commands;
using UserService.Application.Features.SubscriptionTiers.DTOs;
using UserService.Application.Features.SubscriptionTiers.Queries;

namespace UserService.Application.Controllers;

/// <summary>
/// Subscription tier management endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SubscriptionTierController : ControllerBase
{
    private readonly IMediator _mediator;

    public SubscriptionTierController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all subscription tiers (Public)
    /// </summary>
    /// <param name="isActive">Filter by active status (optional)</param>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ResponseModel>> GetTiers([FromQuery] bool? isActive = true)
    {
        var query = new GetSubscriptionTiersQuery { IsActive = isActive };
        var response = await _mediator.Send(query);
        return StatusCode((int)response.Status!, response);
    }

    /// <summary>
    /// Get subscription tier by ID (Public)
    /// </summary>
    /// <param name="id">Tier ID</param>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<ResponseModel>> GetTierById(Guid id)
    {
        var query = new GetSubscriptionTierByIdQuery { Id = id };
        var response = await _mediator.Send(query);
        return StatusCode((int)response.Status!, response);
    }

    /// <summary>
    /// Create new subscription tier (Admin only)
    /// </summary>
    /// <param name="dto">Tier creation data</param>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ResponseModel>> CreateTier([FromBody] CreateSubscriptionTierDto dto)
    {
        var command = new CreateSubscriptionTierCommand { Tier = dto };
        var response = await _mediator.Send(command);
        return StatusCode((int)response.Status!, response);
    }

    /// <summary>
    /// Update subscription tier (Admin only)
    /// </summary>
    /// <param name="id">Tier ID</param>
    /// <param name="dto">Tier update data</param>
    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ResponseModel>> UpdateTier(Guid id, [FromBody] UpdateSubscriptionTierDto dto)
    {
        var command = new UpdateSubscriptionTierCommand { Id = id, Tier = dto };
        var response = await _mediator.Send(command);
        return StatusCode((int)response.Status!, response);
    }

    /// <summary>
    /// Delete subscription tier (Admin only) - Soft delete
    /// </summary>
    /// <param name="id">Tier ID</param>
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ResponseModel>> DeleteTier(Guid id)
    {
        var command = new DeleteSubscriptionTierCommand { Id = id };
        var response = await _mediator.Send(command);
        return StatusCode((int)response.Status!, response);
    }
}
