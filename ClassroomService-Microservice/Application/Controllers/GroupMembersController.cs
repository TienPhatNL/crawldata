using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClassroomService.Application.Features.GroupMembers.Commands;
using ClassroomService.Application.Features.GroupMembers.Queries;
using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Constants;
using System.Security.Claims;
using HttpStatusCodes = Microsoft.AspNetCore.Http.StatusCodes;

namespace ClassroomService.Application.Controllers;

/// <summary>
/// Controller for managing group members
/// </summary>
[ApiController]
[Route("api/group-members")]
[Authorize]
[Produces("application/json")]
[Tags("GroupMembers")]
public class GroupMembersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GroupMembersController> _logger;

    public GroupMembersController(IMediator mediator, ICurrentUserService currentUserService, ILogger<GroupMembersController> logger)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    // ===== QUERY ENDPOINTS =====

    /// <summary>
    /// Get a specific group member by ID
    /// </summary>
    /// <param name="memberId">Group member ID</param>
    /// <returns>Group member details</returns>
    /// <response code="200">Member retrieved successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Member not found</response>
    [HttpGet("{memberId:guid}")]
    [ProducesResponseType(typeof(GetGroupMemberByIdResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetGroupMemberByIdResponse>> GetGroupMemberById(Guid memberId)
    {
        try
        {
            var query = new GetGroupMemberByIdQuery { MemberId = memberId };
            var response = await _mediator.Send(query);

            if (!response.Success)
            {
                if (response.Message.Contains("not found"))
                {
                    return NotFound(response);
                }
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving group member {MemberId}", memberId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = Messages.Error.InternalServerError
            });
        }
    }

    /// <summary>
    /// Get all members in a group
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <returns>List of group members</returns>
    /// <response code="200">Members retrieved successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Group not found</response>
    [HttpGet("groups/{groupId:guid}/members")]
    [ProducesResponseType(typeof(GetGroupMembersResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetGroupMembersResponse>> GetGroupMembers(Guid groupId)
    {
        try
        {
            var query = new GetGroupMembersQuery { GroupId = groupId };
            var response = await _mediator.Send(query);

            if (!response.Success && response.Message.Contains("not found"))
            {
                return NotFound(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving members for group {GroupId}", groupId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = Messages.Error.InternalServerError
            });
        }
    }

    // ===== COMMAND ENDPOINTS =====

    /// <summary>
    /// Add a member to a group (Lecturer only)
    /// </summary>
    /// <param name="command">Member details to add</param>
    /// <returns>Added member information</returns>
    /// <response code="200">Member added successfully</response>
    /// <response code="400">Invalid request or validation failed</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Not authorized - must be course lecturer</response>
    [HttpPost]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(AddGroupMemberResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AddGroupMemberResponse>> AddMember([FromBody] AddGroupMemberCommand command)
    {
        try
        {
            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            _logger.LogInformation("Member {StudentId} added to group {GroupId}", command.StudentId, command.GroupId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding member {StudentId} to group {GroupId}", command.StudentId, command.GroupId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = Messages.Error.InternalServerError
            });
        }
    }

    /// <summary>
    /// Add multiple students to a group at once (Lecturer only)
    /// </summary>
    /// <param name="command">Bulk member addition details</param>
    /// <returns>Bulk addition result with individual student results</returns>
    /// <response code="200">Request processed (check individual results for success/failure)</response>
    /// <response code="400">Invalid request or validation failed</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Not authorized - must be course lecturer</response>
    [HttpPost("bulk")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(AddMultipleGroupMembersResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AddMultipleGroupMembersResponse>> AddMultipleMembers([FromBody] AddMultipleGroupMembersCommand command)
    {
        try
        {
            var response = await _mediator.Send(command);

            _logger.LogInformation("Bulk add request processed for group {GroupId}: {SuccessCount}/{TotalCount} succeeded",
                command.GroupId, response.SuccessCount, response.TotalRequested);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding multiple members to group {GroupId}", command.GroupId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = Messages.Error.InternalServerError
            });
        }
    }

    /// <summary>
    /// Remove a member from a group (Lecturer only)
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <param name="studentId">Student ID to remove</param>
    /// <returns>Removal result</returns>
    /// <response code="200">Member removed successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Not authorized - must be course lecturer</response>
    /// <response code="404">Group or member not found</response>
    [HttpDelete]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(RemoveGroupMemberResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<RemoveGroupMemberResponse>> RemoveMember(
        [FromQuery] Guid groupId, 
        [FromQuery] Guid studentId)
    {
        try
        {
            var command = new RemoveGroupMemberCommand
            {
                GroupId = groupId,
                StudentId = studentId
            };

            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                if (response.Message.Contains("not found"))
                {
                    return NotFound(response);
                }
                return BadRequest(response);
            }

            _logger.LogInformation("Member {StudentId} removed from group {GroupId}", studentId, groupId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing member {StudentId} from group {GroupId}", studentId, groupId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = Messages.Error.InternalServerError
            });
        }
    }

    /// <summary>
    /// Assign a member as group leader (Lecturer only)
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <param name="command">Leader assignment details</param>
    /// <returns>Leader assignment result</returns>
    /// <response code="200">Leader assigned successfully</response>
    /// <response code="400">Invalid request or validation failed</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Not authorized - must be course lecturer</response>
    /// <response code="404">Group or member not found</response>
    [HttpPut("groups/{groupId:guid}/assign-leader")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(AssignGroupLeaderResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssignGroupLeaderResponse>> AssignLeader(
        Guid groupId,
        [FromBody] AssignGroupLeaderCommand command)
    {
        if (groupId != command.GroupId)
        {
            command.GroupId = groupId;
        }

        try
        {
            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                if (response.Message.Contains("not found"))
                {
                    return NotFound(response);
                }
                return BadRequest(response);
            }

            _logger.LogInformation("Leader assigned to group {GroupId}: {StudentId}", groupId, command.StudentId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning leader {StudentId} to group {GroupId}", command.StudentId, groupId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = Messages.Error.InternalServerError
            });
        }
    }

    /// <summary>
    /// Unassign the current group leader (Lecturer only)
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <returns>Leader unassignment result</returns>
    /// <response code="200">Leader unassigned successfully</response>
    /// <response code="400">Invalid request or no leader to unassign</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Not authorized - must be course lecturer</response>
    /// <response code="404">Group not found</response>
    [HttpPut("groups/{groupId:guid}/unassign-leader")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(UnassignGroupLeaderResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<UnassignGroupLeaderResponse>> UnassignLeader(Guid groupId)
    {
        try
        {
            var command = new UnassignGroupLeaderCommand
            {
                GroupId = groupId
            };

            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                if (response.Message.Contains("not found"))
                {
                    return NotFound(response);
                }
                return BadRequest(response);
            }

            _logger.LogInformation("Leader unassigned from group {GroupId}", groupId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unassigning leader from group {GroupId}", groupId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = Messages.Error.InternalServerError
            });
        }
    }

    private Guid GetCurrentUserId()
    {
        if (_currentUserService.UserId.HasValue)
        {
            return _currentUserService.UserId.Value;
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException(Messages.Error.UserIdNotFound);
        }
        return userId;
    }
}
