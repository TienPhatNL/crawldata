using System;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.Common.Models;
using UserService.Application.Features.Subscriptions.Commands;
using UserService.Application.Features.SubscriptionPlans.Commands;
using UserService.Application.Features.SubscriptionPlans.Queries;
using UserService.Application.Features.SubscriptionPlans.DTOs;
using UserService.Application.Features.Users.Queries;

namespace UserService.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionController : ControllerBase
{
    private readonly IMediator _mediator;

    public SubscriptionController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get current user's subscription details
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ResponseModel>> GetSubscription()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Invalid user token" });
        }

        var query = new GetUserProfileQuery { UserId = userId };
        var response = await _mediator.Send(query);
        return StatusCode((int)response.Status!, response);
    }

    /// <summary>
    /// Upgrade subscription tier (Paid Users only)
    /// </summary>
    [HttpPost("upgrade")]
    public async Task<ActionResult<ResponseModel>> UpgradeSubscription([FromBody] UpgradeSubscriptionCommand command)
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
    /// Cancel subscription (downgrade to free tier)
    /// </summary>
    [HttpPost("cancel")]
    public async Task<ActionResult<ResponseModel>> CancelSubscription([FromBody] CancelSubscriptionRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Invalid user token" });
        }

        var command = new CancelSubscriptionCommand
        {
            UserId = userId,
            Reason = request.Reason,
            EffectiveDate = request.EffectiveDate
        };

        var response = await _mediator.Send(command);
        return StatusCode((int)response.Status!, response);
    }

    /// <summary>
    /// Get available subscription plans and pricing
    /// </summary>
    [HttpGet("plans")]
    [AllowAnonymous]
    public async Task<ActionResult<ResponseModel>> GetSubscriptionTiers([FromQuery] bool? isActive)
    {
        var query = new GetSubscriptionPlansQuery { IsActive = isActive };
        var response = await _mediator.Send(query);
        return StatusCode((int)response.Status!, response);
    }

    /// <summary>
    /// Get subscription plan by ID (Public)
    /// </summary>
    /// <param name="id">Plan ID</param>
    [HttpGet("plan/{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<ResponseModel>> GetPlanById(Guid id)
    {
        var query = new GetSubscriptionPlanByIdQuery { Id = id };
        var response = await _mediator.Send(query);
        return StatusCode((int)response.Status!, response);
    }

    /// <summary>
    /// Create a new subscription plan (Admin only)
    /// </summary>
    [HttpPost("plan")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ResponseModel>> CreateSubscriptionPlan([FromBody] CreateSubscriptionPlanDto plan)
    {
        var command = new CreateSubscriptionPlanCommand { Plan = plan };
        var response = await _mediator.Send(command);
        return StatusCode((int)response.Status!, response);
    }

    /// <summary>
    /// Update a subscription plan (Admin only)
    /// </summary>
    [HttpPut("plan/{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ResponseModel>> UpdateSubscriptionPlan(Guid id, [FromBody] UpdateSubscriptionPlanDto plan)
    {
        plan.Id = id;

        var command = new UpdateSubscriptionPlanCommand { Plan = plan };
        var response = await _mediator.Send(command);
        return StatusCode((int)response.Status!, response);
    }

    /// <summary>
    /// Toggle subscription plan active status (Admin only) - Activates or deactivates the plan
    /// </summary>
    [HttpPatch("plan/{id}/toggle")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ResponseModel>> ToggleSubscriptionPlanStatus(Guid id)
    {
        var command = new DeleteSubscriptionPlanCommand { Id = id };
        var response = await _mediator.Send(command);
        return StatusCode((int)response.Status!, response);
    }
}

// Request DTOs
public class CancelSubscriptionRequest
{
    public string? Reason { get; set; }
    public DateTime? EffectiveDate { get; set; }
}
