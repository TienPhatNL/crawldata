using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserService.Application.Common.Models;
using UserService.Application.Features.Authentication.Commands;

namespace UserService.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Register a new user (Lecturer or Paid User self-registration)
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<ResponseModel>> Register([FromBody] RegisterUserCommand command)
    {
        var response = await _mediator.Send(command);
        return StatusCode((int)response.Status!, response);
    }

    /// <summary>
    /// User login with JWT token generation
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ResponseModel>> Login([FromBody] LoginUserCommand command)
    {
        // Add IP address and User Agent from request
        command.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        command.UserAgent = HttpContext.Request.Headers["User-Agent"].FirstOrDefault();

        var response = await _mediator.Send(command);
        return StatusCode((int)response.Status!, response);
    }

    /// <summary>
    /// Google OAuth login with JWT token generation
    /// </summary>
    [HttpPost("google-login")]
    [AllowAnonymous]
    public async Task<ActionResult<ResponseModel>> GoogleLogin([FromBody] GoogleLoginCommand command)
    {
        // Add IP address and User Agent from request
        command.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        command.UserAgent = HttpContext.Request.Headers["User-Agent"].FirstOrDefault();

        var response = await _mediator.Send(command);
        return StatusCode((int)response.Status!, response);
    }

    /// <summary>
    /// Confirm user email address
    /// </summary>
    [HttpPost("confirm-email")]
    [AllowAnonymous]
    public async Task<ActionResult<ResponseModel>> ConfirmEmail([FromBody] ConfirmEmailCommand command)
    {
        var response = await _mediator.Send(command);
        return StatusCode((int)response.Status!, response);
    }

    /// <summary>
    /// Request password reset
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ResponseModel>> ForgotPassword([FromBody] ForgotPasswordCommand command)
    {
        var response = await _mediator.Send(command);
        return StatusCode((int)response.Status!, response);
    }

    /// <summary>
    /// Reset password with token
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ResponseModel>> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        var response = await _mediator.Send(command);
        return StatusCode((int)response.Status!, response);
    }

    /// <summary>
    /// Change password for authenticated user
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<ResponseModel>> ChangePassword([FromBody] ChangePasswordCommand command)
    {
        // Get user ID from JWT claims
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
    /// Refresh JWT token
    /// </summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<ResponseModel>> RefreshToken([FromBody] RefreshTokenCommand command)
    {
        // Add IP address and User Agent from request
        command.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        command.UserAgent = HttpContext.Request.Headers["User-Agent"].FirstOrDefault();

        var response = await _mediator.Send(command);
        return StatusCode((int)response.Status!, response);
    }

    /// <summary>
    /// Logout user and invalidate session
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ResponseModel>> Logout([FromBody] LogoutCommand? command = null)
    {
        // Get user ID from JWT claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Invalid user token" });
        }

        // Create command if not provided
        command ??= new LogoutCommand();
        command.UserId = userId;

        // Get access token from Authorization header
        var authHeader = HttpContext.Request.Headers["User-Agent"].FirstOrDefault();
        if (authHeader?.StartsWith("Bearer ") == true)
        {
            command.AccessToken = authHeader.Substring(7);
        }

        var response = await _mediator.Send(command);
        return StatusCode((int)response.Status!, response);
    }
}