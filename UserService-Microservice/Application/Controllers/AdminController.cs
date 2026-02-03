using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserService.Application.Common.Models;
using UserService.Application.Features.Authentication.Commands;
using UserService.Application.Features.Users.Commands;
using UserService.Application.Features.Users.Queries;
using UserService.Application.Features.Payments.Queries;
using UserService.Domain.Enums;

namespace UserService.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Staff")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all users with filtering and pagination (Admin/Staff only)
    /// </summary>
    [HttpGet("users")]
    public async Task<ActionResult<ResponseModel>> GetUsers([FromQuery] GetUsersQuery query)
    {
        var response = await _mediator.Send(query);
        return StatusCode((int)response.Status!, response);
    }

    /// <summary>
    /// Get specific user details (Admin/Staff only)
    /// </summary>
    [HttpGet("users/{userId:guid}")]
    public async Task<ActionResult<ResponseModel>> GetUser(Guid userId)
    {
        var query = new GetUserProfileQuery { UserId = userId };
        var response = await _mediator.Send(query);
        return StatusCode((int)response.Status!, response);
    }

    /// <summary>
    /// Approve a user account (Staff/Admin only) - Required for Lecturer registration
    /// </summary>
    [HttpPost("users/{userId:guid}/approve")]
    public async Task<ActionResult<ResponseModel>> ApproveUser(Guid userId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var approvedById))
        {
            return Unauthorized(new { message = "Invalid user token" });
        }

        var command = new ApproveUserCommand
        {
            UserId = userId,
            ApprovedBy = approvedById
        };

        var response = await _mediator.Send(command);
        return StatusCode((int)response.Status!, response);
    }

    /// <summary>
    /// Suspend a user account (Admin/Staff only)
    /// </summary>
    [HttpPost("users/{userId:guid}/suspend")]
    public async Task<ActionResult<ResponseModel>> SuspendUser(Guid userId, [FromBody] SuspendUserRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var suspendedById))
        {
            return Unauthorized(new { message = "Invalid user token" });
        }

        var command = new SuspendUserCommand
        {
            UserId = userId,
            SuspendedById = suspendedById,
            Reason = request.Reason,
            SuspendUntil = request.SuspendUntil
        };

        var response = await _mediator.Send(command);
        return StatusCode((int)response.Status!, response);
    }

    /// <summary>
    /// Reactivate a suspended user account (Admin/Staff only)
    /// </summary>
    [HttpPost("users/{userId:guid}/reactivate")]
    public async Task<ActionResult<ResponseModel>> ReactivateUser(Guid userId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var reactivatedById))
        {
            return Unauthorized(new { message = "Invalid user token" });
        }

        var command = new ReactivateUserCommand
        {
            UserId = userId,
            ReactivatedById = reactivatedById
        };

        var response = await _mediator.Send(command);
        return StatusCode((int)response.Status!, response);
    }

    /// <summary>
    /// Update user quota limits (Admin only)
    /// </summary>
    [HttpPut("users/{userId:guid}/quota")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ResponseModel>> UpdateUserQuota(Guid userId, [FromBody] UpdateQuotaRequest request)
    {
        var command = new UpdateUserQuotaCommand
        {
            UserId = userId,
            NewQuotaLimit = request.QuotaLimit
        };

        var response = await _mediator.Send(command);
        return StatusCode((int)response.Status!, response);
    }

    /// <summary>
    /// Reset user quota usage (Admin/Staff only)
    /// </summary>
    [HttpPost("users/{userId:guid}/quota/reset")]
    public async Task<ActionResult<ResponseModel>> ResetUserQuota(Guid userId)
    {
        var command = new ResetUserQuotaCommand { UserId = userId };
        var response = await _mediator.Send(command);
        return StatusCode((int)response.Status!, response);
    }

    /// <summary>
    /// Get users requiring approval (Staff/Admin only)
    /// </summary>
    [HttpGet("users/pending-approval")]
    public async Task<ActionResult<ResponseModel>> GetUsersRequiringApproval()
    {
        var query = new GetUsersQuery
        {
            Status = UserStatus.PendingApproval,
            PageSize = 100
        };

        var response = await _mediator.Send(query);
        return StatusCode((int)response.Status!, response);
    }

    /// <summary>
    /// Get all user payments with comprehensive filtering (Admin only)
    /// </summary>
    /// <param name="userId">Filter by specific user (optional)</param>
    /// <param name="tierId">Filter by subscription tier ID (optional)</param>
    /// <param name="status">Filter by payment status (optional)</param>
    /// <param name="from">Start date (optional)</param>
    /// <param name="to">End date (optional)</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 100)</param>
    /// <returns>Paginated list of payments with user details</returns>
    [HttpGet("payments")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ResponseModel), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<ResponseModel>> GetAllPayments(
        [FromQuery] Guid? userId = null,
        [FromQuery] Guid? tierId = null,
        [FromQuery] SubscriptionPaymentStatus? status = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetSubscriptionPaymentsQuery
        {
            UserId = userId,
            IncludeAllUsers = true, // Admin sees all users
            TierId = tierId,
            Status = status,
            From = from,
            To = to,
            Page = page,
            PageSize = pageSize
        };

        var response = await _mediator.Send(query);
        return StatusCode((int)response.Status!, response);
    }

    /// <summary>
    /// Get payment statistics summary (Admin only)
    /// </summary>
    /// <param name="from">Start date (optional)</param>
    /// <param name="to">End date (optional)</param>
    /// <param name="tierId">Filter by tier (optional)</param>
    [HttpGet("payments/statistics")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ResponseModel>> GetPaymentStatistics(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] Guid? tierId = null)
    {
        var query = new GetSubscriptionPaymentsSummaryQuery
        {
            From = from,
            To = to,
            TierId = tierId
        };

        var response = await _mediator.Send(query);
        return StatusCode((int)response.Status!, response);
    }
}

// Request DTOs
public class SuspendUserRequest
{
    public string Reason { get; set; } = null!;
    public DateTime? SuspendUntil { get; set; }
}

public class UpdateQuotaRequest
{
    public int QuotaLimit { get; set; }
}