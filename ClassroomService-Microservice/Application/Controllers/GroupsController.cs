using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClassroomService.Application.Features.Groups.Commands;
using ClassroomService.Application.Features.Groups.Queries;
using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Constants;
using System.Security.Claims;
using HttpStatusCodes = Microsoft.AspNetCore.Http.StatusCodes;

namespace ClassroomService.Application.Controllers;

/// <summary>
/// Controller for managing groups within courses
/// </summary>
[ApiController]
[Route("api/groups")]
[Authorize]
[Produces("application/json")]
[Tags("Groups")]
public class GroupsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GroupsController> _logger;

    public GroupsController(IMediator mediator, ICurrentUserService currentUserService, ILogger<GroupsController> logger)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    // ===== QUERY ENDPOINTS =====

    /// <summary>
    /// Get a specific group by ID
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <returns>Group details</returns>
    /// <response code="200">Group retrieved successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Group not found</response>
    [HttpGet("{groupId:guid}")]
    [ProducesResponseType(typeof(GetGroupByIdResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetGroupByIdResponse>> GetGroupById(Guid groupId)
    {
        try
        {
            var query = new GetGroupByIdQuery { GroupId = groupId };
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
            _logger.LogError(ex, "Error retrieving group {GroupId}", groupId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = Messages.Error.InternalServerError
            });
        }
    }

    /// <summary>
    /// Get all groups in a course
    /// </summary>
    /// <param name="courseId">Course ID</param>
    /// <returns>List of groups in the course</returns>
    /// <response code="200">Groups retrieved successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Course not found</response>
    [HttpGet("courses/{courseId:guid}")]
    [ProducesResponseType(typeof(GetCourseGroupsResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetCourseGroupsResponse>> GetCourseGroups(Guid courseId)
    {
        try
        {
            var query = new GetCourseGroupsQuery { CourseId = courseId };
            var response = await _mediator.Send(query);

            if (!response.Success && response.Message.Contains("not found"))
            {
                return NotFound(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving groups for course {CourseId}", courseId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = Messages.Error.InternalServerError
            });
        }
    }

    /// <summary>
    /// Get groups that the current student belongs to (Students only)
    /// </summary>
    /// <param name="courseId">Optional course ID to filter groups by specific course</param>
    /// <returns>List of groups the student is a member of</returns>
    /// <response code="200">Groups retrieved successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("my-groups")]
    [Authorize(Roles = RoleConstants.Student)]
    [ProducesResponseType(typeof(GetMyGroupsResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GetMyGroupsResponse>> GetMyGroups([FromQuery] Guid? courseId = null)
    {
        try
        {
            var userId = GetCurrentUserId();

            var query = new GetMyGroupsQuery
            {
                StudentId = userId,
                CourseId = courseId
            };

            var response = await _mediator.Send(query);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving groups for student");
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = Messages.Error.InternalServerError
            });
        }
    }

    // ===== COMMAND ENDPOINTS =====

    /// <summary>
    /// Create a new group in a course (Lecturer only)
    /// </summary>
    /// <param name="command">Group creation details</param>
    /// <returns>Created group information</returns>
    /// <response code="200">Group created successfully</response>
    /// <response code="400">Invalid request or validation failed</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Not authorized - must be course lecturer</response>
    [HttpPost]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(CreateGroupResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CreateGroupResponse>> CreateGroup([FromBody] CreateGroupCommand command)
    {
        try
        {
            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            _logger.LogInformation("Group {GroupId} created successfully in course {CourseId}", response.GroupId, command.CourseId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating group in course {CourseId}", command.CourseId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = Messages.Error.InternalServerError
            });
        }
    }

    /// <summary>
    /// Update an existing group (Lecturer only)
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <param name="command">Updated group details</param>
    /// <returns>Updated group information</returns>
    /// <response code="200">Group updated successfully</response>
    /// <response code="400">Invalid request or validation failed</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Not authorized - must be course lecturer</response>
    /// <response code="404">Group not found</response>
    [HttpPut("{groupId:guid}")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(UpdateGroupResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<UpdateGroupResponse>> UpdateGroup(Guid groupId, [FromBody] UpdateGroupCommand command)
    {
        command.GroupId = groupId;
        //if (groupId != command.GroupId)
        //{
        //    return BadRequest(new { success = false, message = "Group ID mismatch" });
        //}

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

            _logger.LogInformation("Group {GroupId} updated successfully", groupId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating group {GroupId}", groupId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = Messages.Error.InternalServerError
            });
        }
    }

    /// <summary>
    /// Delete a group (Lecturer only)
    /// Deletes all group members first, then deletes the group.
    /// Cannot delete a group that has been assigned to an assignment.
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <returns>Deletion result</returns>
    /// <response code="200">Group and all its members deleted successfully</response>
    /// <response code="400">Cannot delete - group is assigned to an assignment</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Not authorized - must be course lecturer</response>
    /// <response code="404">Group not found</response>
    [HttpDelete("{groupId:guid}")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(DeleteGroupResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeleteGroupResponse>> DeleteGroup(Guid groupId)
    {
        try
        {
            var command = new DeleteGroupCommand { GroupId = groupId };
            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                if (response.Message.Contains("not found"))
                {
                    return NotFound(response);
                }
                return BadRequest(response);
            }

            _logger.LogInformation("Group {GroupId} deleted successfully", groupId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting group {GroupId}", groupId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = Messages.Error.InternalServerError
            });
        }
    }

    /// <summary>
    /// Randomize enrolled students into groups (Lecturer only)
    /// </summary>
    /// <param name="command">Randomization settings including course ID and group size</param>
    /// <returns>Randomization result with created groups</returns>
    /// <response code="200">Students randomized successfully</response>
    /// <response code="400">Invalid request or validation failed</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Not authorized - must be course lecturer</response>
    /// <response code="404">Course not found</response>
    [HttpPost("randomize")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(RandomizeStudentsToGroupsResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<RandomizeStudentsToGroupsResponse>> RandomizeStudentsToGroups(
        [FromBody] RandomizeStudentsToGroupsCommand command)
    {
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

            _logger.LogInformation("Randomized {StudentCount} students into {GroupCount} groups for course {CourseId}",
                response.StudentsAssigned, response.GroupsCreated, command.CourseId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error randomizing students for course {CourseId}", command.CourseId);
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
