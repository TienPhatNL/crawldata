using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClassroomService.Application.Features.Assignments.Commands;
using ClassroomService.Application.Features.Assignments.Queries;
using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.Enums;
using System.Security.Claims;
using HttpStatusCodes = Microsoft.AspNetCore.Http.StatusCodes;

namespace ClassroomService.Application.Controllers;

/// <summary>
/// Controller for managing course assignments
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
[Tags("Assignments")]
public class AssignmentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AssignmentsController> _logger;

    public AssignmentsController(
        IMediator mediator,
        ICurrentUserService currentUserService,
        ILogger<AssignmentsController> logger)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    // ===== QUERY ENDPOINTS =====

    /// <summary>
    /// Get a specific assignment by ID
    /// </summary>
    /// <param name="id">Assignment ID</param>
    /// <returns>Assignment details</returns>
    /// <response code="200">Assignment retrieved successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Assignment not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GetAssignmentByIdResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetAssignmentByIdResponse>> GetAssignmentById(Guid id)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

            var query = new GetAssignmentByIdQuery 
            { 
                AssignmentId = id,
                RequestUserId = currentUserId,
                RequestUserRole = currentUserRole
            };
            var response = await _mediator.Send(query);

            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving assignment {AssignmentId}", id);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = Messages.Error.InternalServerError
            });
        }
    }

    /// <summary>
    /// Get assignments for a course with filtering and pagination
    /// </summary>
    /// <param name="courseId">Course ID (required)</param>
    /// <param name="statuses">Filter by status (array of integers: 0=Draft, 1=Active, 2=Extended, 3=Overdue, 4=Closed)</param>
    /// <param name="isGroupAssignment">Filter by group assignment flag</param>
    /// <param name="assignedToGroupId">Filter assignments assigned to specific group</param>
    /// <param name="hasAssignedGroups">Filter by whether assignment has groups assigned</param>
    /// <param name="dueDateFrom">Filter assignments due after this date</param>
    /// <param name="dueDateTo">Filter assignments due before this date</param>
    /// <param name="isUpcoming">Filter upcoming assignments (due within 7 days)</param>
    /// <param name="isOverdue">Filter overdue assignments</param>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="sortBy">Sort by field (DueDate, Title, CreatedAt, Status)</param>
    /// <param name="sortOrder">Sort order (asc, desc)</param>
    /// <param name="searchQuery">Search in title and description</param>
    /// <returns>Paginated list of assignments</returns>
    /// <response code="200">Assignments retrieved successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet]
    [ProducesResponseType(typeof(GetAssignmentsResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GetAssignmentsResponse>> GetAssignments(
        [FromQuery] Guid courseId,
        [FromQuery] int[]? statuses = null,
        [FromQuery] bool? isGroupAssignment = null,
        [FromQuery] Guid? assignedToGroupId = null,
        [FromQuery] bool? hasAssignedGroups = null,
        [FromQuery] DateTime? dueDateFrom = null,
        [FromQuery] DateTime? dueDateTo = null,
        [FromQuery] bool? isUpcoming = null,
        [FromQuery] bool? isOverdue = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "DueDate",
        [FromQuery] string sortOrder = "asc",
        [FromQuery] string? searchQuery = null)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

            // Convert int array to AssignmentStatus list
            List<AssignmentStatus>? statusList = null;
            if (statuses != null && statuses.Length > 0)
            {
                statusList = statuses.Select(s => (AssignmentStatus)s).ToList();
            }

            var query = new GetAssignmentsQuery
            {
                RequestUserId = currentUserId,
                RequestUserRole = currentUserRole,
                CourseId = courseId,
                Statuses = statusList,
                IsGroupAssignment = isGroupAssignment,
                AssignedToGroupId = assignedToGroupId,
                HasAssignedGroups = hasAssignedGroups,
                DueDateFrom = dueDateFrom,
                DueDateTo = dueDateTo,
                IsUpcoming = isUpcoming,
                IsOverdue = isOverdue,
                PageNumber = pageNumber,
                PageSize = pageSize,
                SortBy = sortBy,
                SortOrder = sortOrder,
                SearchQuery = searchQuery
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
            _logger.LogError(ex, "Error retrieving assignments for course {CourseId}", courseId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = Messages.Error.InternalServerError
            });
        }
    }

    /// <summary>
    /// Get all assignments belonging to a specific topic with filtering and pagination
    /// </summary>
    /// <param name="topicId">Topic ID</param>
    /// <param name="courseId">Optional: Filter by specific course</param>
    /// <param name="statuses">Filter by status (array of integers)</param>
    /// <param name="isGroupAssignment">Filter by assignment type</param>
    /// <param name="dueDateFrom">Filter due date from</param>
    /// <param name="dueDateTo">Filter due date to</param>
    /// <param name="isUpcoming">Filter upcoming assignments</param>
    /// <param name="isOverdue">Filter overdue assignments</param>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="sortBy">Sort by field</param>
    /// <param name="sortOrder">Sort order</param>
    /// <param name="searchQuery">Search query</param>
    /// <returns>Assignments for the topic</returns>
    /// <response code="200">Assignments retrieved successfully</response>
    /// <response code="400">Invalid parameters</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Topic not found</response>
    [HttpGet("by-topic/{topicId:guid}")]
    [ProducesResponseType(typeof(GetAssignmentsByTopicResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetAssignmentsByTopicResponse>> GetAssignmentsByTopic(
        Guid topicId,
        [FromQuery] Guid? courseId = null,
        [FromQuery] int[]? statuses = null,
        [FromQuery] bool? isGroupAssignment = null,
        [FromQuery] DateTime? dueDateFrom = null,
        [FromQuery] DateTime? dueDateTo = null,
        [FromQuery] bool? isUpcoming = null,
        [FromQuery] bool? isOverdue = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "DueDate",
        [FromQuery] string sortOrder = "asc",
        [FromQuery] string? searchQuery = null)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

            // Convert int array to AssignmentStatus list
            List<AssignmentStatus>? statusList = null;
            if (statuses != null && statuses.Length > 0)
            {
                statusList = statuses.Select(s => (AssignmentStatus)s).ToList();
            }

            var query = new GetAssignmentsByTopicQuery
            {
                RequestUserId = currentUserId,
                RequestUserRole = currentUserRole,
                TopicId = topicId,
                CourseId = courseId,
                Statuses = statusList,
                IsGroupAssignment = isGroupAssignment,
                DueDateFrom = dueDateFrom,
                DueDateTo = dueDateTo,
                IsUpcoming = isUpcoming,
                IsOverdue = isOverdue,
                PageNumber = pageNumber,
                PageSize = pageSize,
                SortBy = sortBy,
                SortOrder = sortOrder,
                SearchQuery = searchQuery
            };

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
            _logger.LogError(ex, "Error retrieving assignments for topic {TopicId}", topicId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = Messages.Error.InternalServerError
            });
        }
    }

    /// <summary>
    /// Get my assignments from all enrolled courses (Student only)
    /// </summary>
    /// <param name="courseId">Optional: Filter by specific course</param>
    /// <param name="statuses">Filter by status (array of integers: 0=Draft, 1=Active, 2=Extended, 3=Overdue, 4=Closed)</param>
    /// <param name="isUpcoming">Filter upcoming assignments</param>
    /// <param name="isOverdue">Filter overdue assignments</param>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="sortBy">Sort by field</param>
    /// <param name="sortOrder">Sort order</param>
    /// <returns>My assignments</returns>
    /// <response code="200">Assignments retrieved successfully</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("my-assignments")]
    [Authorize(Roles = RoleConstants.Student)]
    [ProducesResponseType(typeof(GetMyAssignmentsResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GetMyAssignmentsResponse>> GetMyAssignments(
        [FromQuery] Guid? courseId = null,
        [FromQuery] int[]? statuses = null,
        [FromQuery] bool? isUpcoming = null,
        [FromQuery] bool? isOverdue = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "DueDate",
        [FromQuery] string sortOrder = "asc")
    {
        try
        {
            var studentId = GetCurrentUserId();
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

            // Convert int array to AssignmentStatus list
            List<AssignmentStatus>? statusList = null;
            if (statuses != null && statuses.Length > 0)
            {
                statusList = statuses.Select(s => (AssignmentStatus)s).ToList();
            }

            var query = new GetMyAssignmentsQuery
            {
                StudentId = studentId,
                RequestUserRole = currentUserRole,
                CourseId = courseId,
                Statuses = statusList,
                IsUpcoming = isUpcoming,
                IsOverdue = isOverdue,
                PageNumber = pageNumber,
                PageSize = pageSize,
                SortBy = sortBy,
                SortOrder = sortOrder
            };

            var response = await _mediator.Send(query);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving my assignments");
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = Messages.Error.InternalServerError
            });
        }
    }

    /// <summary>
    /// Get assignment statistics for a course (Lecturer only)
    /// </summary>
    /// <param name="courseId">Course ID</param>
    /// <returns>Assignment statistics</returns>
    /// <response code="200">Statistics retrieved successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Lecturer only</response>
    /// <response code="404">Course not found</response>
    [HttpGet("courses/{courseId:guid}/statistics")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(GetAssignmentStatisticsResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetAssignmentStatisticsResponse>> GetAssignmentStatistics(Guid courseId)
    {
        try
        {
            var query = new GetAssignmentStatisticsQuery { CourseId = courseId };
            var response = await _mediator.Send(query);

            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving statistics for course {CourseId}", courseId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = Messages.Error.InternalServerError
            });
        }
    }

    /// <summary>
    /// Get grade statistics for authenticated student in a specific course
    /// </summary>
    /// <param name="courseId">Course ID</param>
    /// <returns>Student grade statistics including weighted grades and GPA</returns>
    /// <response code="200">Statistics retrieved successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Student only</response>
    /// <response code="404">Course or enrollment not found</response>
    [HttpGet("courses/{courseId:guid}/students/grades")]
    [Authorize(Roles = RoleConstants.Student)]
    [ProducesResponseType(typeof(GetStudentGradeStatisticsResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetStudentGradeStatisticsResponse>> GetStudentGradeStatistics(Guid courseId)
    {
        try
        {
            var userId = _currentUserService.UserId;
            var userRole = _currentUserService.Role;

            if (!userId.HasValue)
            {
                return Unauthorized(new { success = false, message = "User ID not found" });
            }

            var query = new GetStudentGradeStatisticsQuery(
                CourseId: courseId,
                RequestUserId: userId.Value,
                RequestUserRole: userRole ?? RoleConstants.Student
            );

            var response = await _mediator.Send(query);

            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving grade statistics for student in course {CourseId}", courseId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = Messages.Error.InternalServerError
            });
        }
    }

    // ===== COMMAND ENDPOINTS =====

    /// <summary>
    /// Creates a new assignment for a course (Lecturer only)
    /// Note: GroupIds is optional and only applicable if IsGroupAssignment is true
    /// </summary>
    /// <param name="command">The assignment creation details</param>
    /// <returns>The assignment creation response</returns>
    /// <response code="200">Assignment created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Lecturer only</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(CreateAssignmentResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CreateAssignmentResponse>> CreateAssignment([FromBody] CreateAssignmentCommand command)
    {
        try
        {
            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            _logger.LogInformation("Assignment created successfully with ID: {AssignmentId}", response.AssignmentId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating assignment for course {CourseId}", command.CourseId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = Messages.Error.InternalServerError
            });
        }
    }

    /// <summary>
    /// Schedule or unschedule an assignment (Lecturer only)
    /// - Schedule: Draft -> Scheduled (or Active if StartDate is now)
    /// - Unschedule: Scheduled -> Draft
    /// Validates that StartDate is not in the past when scheduling.
    /// </summary>
    /// <param name="id">Assignment ID</param>
    /// <param name="command">Schedule command (Schedule: true to schedule, false to unschedule)</param>
    /// <returns>Updated assignment with new status</returns>
    /// <response code="200">Assignment scheduled/unscheduled successfully</response>
    /// <response code="400">Invalid request or status restrictions violated</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Lecturer only</response>
    /// <response code="404">Assignment not found</response>
    [HttpPost("{id:guid}/schedule")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(ScheduleAssignmentResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScheduleAssignmentResponse>> ScheduleAssignment(
        Guid id,
        [FromBody] ScheduleAssignmentCommand command)
    {
        command.AssignmentId = id;

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

            _logger.LogInformation("Assignment {AssignmentId} {Action}", 
                id, command.Schedule ? "scheduled" : "unscheduled");
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling/unscheduling assignment {AssignmentId}", id);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = Messages.Error.InternalServerError
            });
        }
    }

    /// <summary>
    /// Update an assignment (Lecturer only)
    /// Note: Can only update Draft assignments fully. For Active/Extended/Overdue assignments, 
    /// dates (StartDate, DueDate) cannot be updated - use ExtendDueDate endpoint instead.
    /// Closed assignments cannot be updated at all.
    /// </summary>
    /// <param name="id">Assignment ID</param>
    /// <param name="command">Updated assignment details</param>
    /// <returns>Updated assignment</returns>
    /// <response code="200">Assignment updated successfully</response>
    /// <response code="400">Invalid request or status restrictions violated</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Lecturer only</response>
    /// <response code="404">Assignment not found</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(UpdateAssignmentResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<UpdateAssignmentResponse>> UpdateAssignment(
        Guid id,
        [FromBody] UpdateAssignmentCommand command)
    {
        command.AssignmentId = id;

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

            _logger.LogInformation("Assignment {AssignmentId} updated successfully", id);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating assignment {AssignmentId}", id);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = Messages.Error.InternalServerError
            });
        }
    }

    /// <summary>
    /// Delete an assignment (Lecturer only)
    /// Can only delete assignments in Draft status
    /// </summary>
    /// <param name="id">Assignment ID</param>
    /// <returns>Deletion result</returns>
    /// <response code="200">Assignment deleted successfully</response>
    /// <response code="400">Cannot delete - assignment not in Draft status</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Lecturer only</response>
    /// <response code="404">Assignment not found</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(DeleteAssignmentResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeleteAssignmentResponse>> DeleteAssignment(Guid id)
    {
        try
        {
            var command = new DeleteAssignmentCommand { AssignmentId = id };
            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                if (response.Message.Contains("not found"))
                {
                    return NotFound(response);
                }
                return BadRequest(response);
            }

            _logger.LogInformation("Assignment {AssignmentId} deleted successfully", id);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting assignment {AssignmentId}", id);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = Messages.Error.InternalServerError
            });
        }
    }

    /// <summary>
    /// Extend the due date of an assignment (Lecturer only)
    /// </summary>
    /// <param name="id">Assignment ID</param>
    /// <param name="command">Extended due date details</param>
    /// <returns>Updated assignment</returns>
    /// <response code="200">Due date extended successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Lecturer only</response>
    /// <response code="404">Assignment not found</response>
    [HttpPatch("{id:guid}/extend-due-date")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(ExtendDueDateResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExtendDueDateResponse>> ExtendDueDate(
        Guid id,
        [FromBody] ExtendDueDateCommand command)
    {
        command.AssignmentId = id;

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

            _logger.LogInformation("Assignment {AssignmentId} due date extended to {ExtendedDueDate}",
                id, command.ExtendedDueDate);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extending due date for assignment {AssignmentId}", id);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = Messages.Error.InternalServerError
            });
        }
    }

    /// <summary>
    /// Close an assignment manually (Lecturer only)
    /// Cannot close assignments in Draft status
    /// </summary>
    /// <param name="id">Assignment ID</param>
    /// <returns>Closed assignment</returns>
    /// <response code="200">Assignment closed successfully</response>
    /// <response code="400">Invalid request or Draft status</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Lecturer only</response>
    /// <response code="404">Assignment not found</response>
    [HttpPatch("{id:guid}/close")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(CloseAssignmentResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<CloseAssignmentResponse>> CloseAssignment(Guid id)
    {
        try
        {
            var command = new CloseAssignmentCommand { AssignmentId = id };
            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                if (response.Message.Contains("not found"))
                {
                    return NotFound(response);
                }
                return BadRequest(response);
            }

            _logger.LogInformation("Assignment {AssignmentId} closed successfully", id);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing assignment {AssignmentId}", id);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = Messages.Error.InternalServerError
            });
        }
    }

    // ===== GROUP MANAGEMENT ENDPOINTS =====

    /// <summary>
    /// Assign groups to a group assignment (Lecturer only)
    /// </summary>
    /// <param name="command">Assignment ID and Group IDs to assign</param>
    /// <returns>Assigned groups</returns>
    /// <response code="200">Groups assigned successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Lecturer only</response>
    /// <response code="404">Assignment not found</response>
    [HttpPost("assign-groups")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(AssignGroupsResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssignGroupsResponse>> AssignGroups(
        [FromBody] AssignGroupsCommand command)
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

            _logger.LogInformation("Assigned {Count} groups to assignment {AssignmentId}", 
                response.AssignedCount, command.AssignmentId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning groups to assignment {AssignmentId}", command.AssignmentId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = Messages.Error.InternalServerError
            });
        }
    }

    /// <summary>
    /// Unassign groups from an assignment (Lecturer only)
    /// </summary>
    /// <param name="command">Assignment ID and Group IDs to unassign</param>
    /// <returns>Unassignment result</returns>
    /// <response code="200">Groups unassigned successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Lecturer only</response>
    /// <response code="404">Assignment not found</response>
    [HttpPost("unassign-groups")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(UnassignGroupsResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<UnassignGroupsResponse>> UnassignGroups(
        [FromBody] UnassignGroupsCommand command)
    {
        try
        {
            // Set the user who is unassigning
            var currentUserId = GetCurrentUserId();
            command.UnassignedBy = currentUserId;
            
            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                if (response.Message.Contains("not found"))
                {
                    return NotFound(response);
                }
                return BadRequest(response);
            }

            _logger.LogInformation("Unassigned {Count} groups from assignment {AssignmentId}", 
                response.UnassignedCount, command.AssignmentId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unassigning groups from assignment {AssignmentId}", command.AssignmentId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = Messages.Error.InternalServerError
            });
        }
    }

    /// <summary>
    /// Get groups assigned to an assignment
    /// </summary>
    /// <param name="id">Assignment ID</param>
    /// <returns>List of assigned groups</returns>
    /// <response code="200">Groups retrieved successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Assignment not found</response>
    [HttpGet("{id:guid}/groups")]
    [ProducesResponseType(typeof(GetAssignmentGroupsResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetAssignmentGroupsResponse>> GetAssignmentGroups(Guid id)
    {
        try
        {
            var query = new GetAssignmentGroupsQuery { AssignmentId = id };
            var response = await _mediator.Send(query);

            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving groups for assignment {AssignmentId}", id);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = Messages.Error.InternalServerError
            });
        }
    }

    /// <summary>
    /// Get unassigned groups in a course (Lecturer only)
    /// </summary>
    /// <param name="courseId">Course ID</param>
    /// <returns>List of groups without assignments</returns>
    /// <response code="200">Unassigned groups retrieved successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Lecturer only</response>
    /// <response code="404">Course not found</response>
    [HttpGet("courses/{courseId:guid}/unassigned-groups")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(GetUnassignedGroupsResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetUnassignedGroupsResponse>> GetUnassignedGroups(Guid courseId)
    {
        try
        {
            var query = new GetUnassignedGroupsQuery { CourseId = courseId };
            var response = await _mediator.Send(query);

            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unassigned groups for course {CourseId}", courseId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = Messages.Error.InternalServerError
            });
        }
    }

    /// <summary>
    /// Get assignment for a specific group
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <returns>Assignment assigned to the group (if any)</returns>
    /// <response code="200">Assignment retrieved successfully (or no assignment)</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Group not found</response>
    [HttpGet("groups/{groupId:guid}/assignment")]
    [ProducesResponseType(typeof(GetGroupAssignmentResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetGroupAssignmentResponse>> GetGroupAssignment(Guid groupId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

            var query = new GetGroupAssignmentQuery 
            { 
                GroupId = groupId,
                RequestUserId = currentUserId,
                RequestUserRole = currentUserRole
            };
            var response = await _mediator.Send(query);

            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving assignment for group {GroupId}", groupId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = Messages.Error.InternalServerError
            });
        }
    }

    // ===== ATTACHMENT MANAGEMENT ENDPOINTS =====

    /// <summary>
    /// Upload file attachments to an assignment (Lecturer only)
    /// </summary>
    /// <param name="id">Assignment ID</param>
    /// <param name="files">Files to upload (instructions, reference materials, etc.)</param>
    /// <returns>Upload result with file metadata</returns>
    /// <response code="200">Files uploaded successfully</response>
    /// <response code="400">Invalid files or validation error</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Only course lecturer</response>
    /// <response code="404">Assignment not found</response>
    [HttpPost("{id:guid}/attachments")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(UploadAssignmentFilesResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<UploadAssignmentFilesResponse>> UploadAssignmentFiles(
        Guid id,
        [FromForm] List<IFormFile> files)
    {
        try
        {
            var command = new UploadAssignmentFilesCommand
            {
                AssignmentId = id,
                Files = files
            };

            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading files to assignment {AssignmentId}", id);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = Messages.Error.InternalServerError
            });
        }
    }

    /// <summary>
    /// Delete a specific file attachment from an assignment (Lecturer only)
    /// </summary>
    /// <param name="id">Assignment ID</param>
    /// <param name="fileId">File attachment ID to delete</param>
    /// <returns>Deletion result</returns>
    /// <response code="200">File deleted successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Only course lecturer</response>
    /// <response code="404">Assignment or file not found</response>
    [HttpDelete("{id:guid}/attachments/{fileId:guid}")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(DeleteAssignmentFileResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeleteAssignmentFileResponse>> DeleteAssignmentFile(
        Guid id,
        Guid fileId)
    {
        try
        {
            var command = new DeleteAssignmentFileCommand
            {
                AssignmentId = id,
                FileId = fileId
            };

            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                if (response.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    return NotFound(response);
                }
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FileId} from assignment {AssignmentId}", fileId, id);
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