using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.Common.Models;
using UserService.Application.Features.Users.Commands;
using UserService.Application.Features.Users.Queries;

namespace UserService.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get current user profile
    /// </summary>
    [HttpGet("profile")]
    public async Task<ActionResult<ResponseModel>> GetProfile()
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
    /// Update current user profile
    /// </summary>
    [HttpPut("profile")]
    public async Task<ActionResult<ResponseModel>> UpdateProfile([FromBody] UpdateUserProfileCommand command)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Invalid user token" });
        }

        command.SetUserId(userId);
        var response = await _mediator.Send(command);
        return StatusCode((int)response.Status!, response);
    }

    /// <summary>
    /// Upload or replace the current user's profile picture
    /// </summary>
    [HttpPost("profile/picture")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ResponseModel>> UploadProfilePicture([FromForm] UploadProfilePictureRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Invalid user token" });
        }

        if (request?.ProfilePicture == null || request.ProfilePicture.Length == 0)
        {
            return BadRequest(new { message = "Profile picture file is required" });
        }

        var command = new UploadProfilePictureCommand(userId)
        {
            ProfilePicture = request.ProfilePicture
        };

        var response = await _mediator.Send(command);
        return StatusCode((int)response.Status!, response);
    }

    /// <summary>
    /// Get user quota information
    /// </summary>
    [HttpGet("quota")]
    public async Task<ActionResult<ResponseModel>> GetQuotaInfo()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Invalid user token" });
        }

        var query = new GetUserQuotaQuery { UserId = userId };
        var response = await _mediator.Send(query);
        return StatusCode((int)response.Status!, response);
    }
}

public class UploadProfilePictureRequest
{
    /// <summary>
    /// Image file (max 5 MB) to set as the user's profile picture.
    /// </summary>
    [Required]
    public IFormFile? ProfilePicture { get; set; }
}
