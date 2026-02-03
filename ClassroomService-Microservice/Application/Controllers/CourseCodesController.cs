using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClassroomService.Application.Features.CourseCodes.Commands;
using ClassroomService.Application.Features.CourseCodes.Queries;
using ClassroomService.Application.Features.CourseCodes.DTOs;
using ClassroomService.Domain.Constants;
using HttpStatusCodes = Microsoft.AspNetCore.Http.StatusCodes;

namespace ClassroomService.Application.Controllers;

/// <summary>
/// Controller for managing course codes (Staff only)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Course Codes")]
[Authorize] // Require authentication for all endpoints
public class CourseCodesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<CourseCodesController> _logger;

    /// <summary>
    /// Initializes a new instance of the CourseCodesController
    /// </summary>
    /// <param name="mediator">The mediator instance</param>
    /// <param name="logger">The logger instance</param>
    public CourseCodesController(IMediator mediator, ILogger<CourseCodesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new course code (Staff only)
    /// </summary>
    /// <param name="command">The course code creation details</param>
    /// <returns>The course code creation response</returns>
    /// <response code="200">Course code created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Staff role required</response>
    /// <response code="409">Course code already exists</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    [Authorize(Roles = RoleConstants.Staff)]
    [ProducesResponseType(typeof(CreateCourseCodeResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status409Conflict)]
    [ProducesResponseType(HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CreateCourseCodeResponse>> CreateCourseCode([FromBody] CreateCourseCodeCommand command)
    {
        try
        {
            var response = await _mediator.Send(command);
            
            if (!response.Success)
            {
                if (response.Message.Contains("already exists"))
                {
                    return Conflict(response);
                }
                return BadRequest(response);
            }

            _logger.LogInformation("Course code created successfully: {Code}", command.Code);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating course code {Code}", command.Code);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = Messages.Error.InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets a course code by ID (All authenticated users)
    /// </summary>
    /// <param name="id">The course code ID</param>
    /// <returns>The course code details</returns>
    /// <response code="200">Course code found</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Course code not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(GetCourseCodeResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GetCourseCodeResponse>> GetCourseCode(Guid id)
    {
        try
        {
            var query = new GetCourseCodeQuery { Id = id };
            var response = await _mediator.Send(query);
            
            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving course code {Id}", id);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = Messages.Error.InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets all course codes with filtering and pagination (All authenticated users)
    /// </summary>
    /// <param name="code">Search by course code (partial match)</param>
    /// <param name="title">Search by course title (partial match)</param>
    /// <param name="department">Filter by department</param>
    /// <param name="isActive">Filter by active status</param>
    /// <param name="createdAfter">Filter course codes created after this date</param>
    /// <param name="createdBefore">Filter course codes created before this date</param>
    /// <param name="hasActiveCourses">Filter by whether course code has active courses</param>
    /// <param name="page">Page number for pagination (1-based)</param>
    /// <param name="pageSize">Number of items per page (max 100)</param>
    /// <param name="sortBy">Sort field (Code, Title, Department, CreatedAt, ActiveCoursesCount)</param>
    /// <param name="sortDirection">Sort direction (asc or desc)</param>
    /// <returns>Filtered and paginated list of course codes</returns>
    /// <response code="200">Course codes retrieved successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(GetAllCourseCodesResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GetAllCourseCodesResponse>> GetAllCourseCodes(
        [FromQuery] string? code = null,
        [FromQuery] string? title = null,
        [FromQuery] string? department = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] DateTime? createdAfter = null,
        [FromQuery] DateTime? createdBefore = null,
        [FromQuery] bool? hasActiveCourses = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = "Code",
        [FromQuery] string sortDirection = "asc")
    {
        try
        {
            var query = new GetAllCourseCodesQuery
            {
                Filter = new CourseCodeFilterDto
                {
                    Code = code,
                    Title = title,
                    Department = department,
                    IsActive = isActive,
                    CreatedAfter = createdAfter,
                    CreatedBefore = createdBefore,
                    HasActiveCourses = hasActiveCourses,
                    Page = page,
                    PageSize = pageSize,
                    SortBy = sortBy,
                    SortDirection = sortDirection
                }
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
            _logger.LogError(ex, "Error retrieving course codes");
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = Messages.Error.InternalServerError
            });
        }
    }

    /// <summary>
    /// Updates an existing course code (Staff only)
    /// </summary>
    /// <param name="id">The course code ID</param>
    /// <param name="command">The course code update details</param>
    /// <returns>The updated course code</returns>
    /// <response code="200">Course code updated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Staff role required</response>
    /// <response code="404">Course code not found</response>
    /// <response code="409">Course code already exists</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("{id}")]
    [Authorize(Roles = RoleConstants.Staff)]
    [ProducesResponseType(typeof(UpdateCourseCodeResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpStatusCodes.Status409Conflict)]
    [ProducesResponseType(HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UpdateCourseCodeResponse>> UpdateCourseCode(Guid id, [FromBody] UpdateCourseCodeCommand command)
    {
        try
        {
            command.Id = id; // Ensure the ID matches the route
            var response = await _mediator.Send(command);
            
            if (!response.Success)
            {
                if (response.Message.Contains("not found"))
                {
                    return NotFound(response);
                }
                if (response.Message.Contains("already exists"))
                {
                    return Conflict(response);
                }
                return BadRequest(response);
            }

            _logger.LogInformation("Course code updated successfully: {Id}", id);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating course code {Id}", id);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = Messages.Error.InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets a simplified list of course codes for selection dropdowns (All authenticated users)
    /// </summary>
    /// <param name="activeOnly">Whether to include only active course codes</param>
    /// <param name="department">Filter by department</param>
    /// <returns>Simplified list of course codes for selection</returns>
    /// <response code="200">Course code options retrieved successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("options")]
    [ProducesResponseType(typeof(List<CourseCodeSummaryDto>), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<CourseCodeSummaryDto>>> GetCourseCodeOptions(
        [FromQuery] bool activeOnly = true,
        [FromQuery] string? department = null)
    {
        try
        {
            var query = new GetAllCourseCodesQuery
            {
                Filter = new CourseCodeFilterDto
                {
                    IsActive = activeOnly ? true : false,
                    Department = department,
                    Page = 1,
                    PageSize = 1000, // Get all for options
                    SortBy = "Code",
                    SortDirection = "asc"
                }
            };

            var response = await _mediator.Send(query);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }

            var options = response.CourseCodes.Select(cc => new CourseCodeSummaryDto
            {
                Id = cc.Id,
                Code = cc.Code,
                Title = cc.Title,
                Department = cc.Department,
                IsActive = cc.IsActive
            }).ToList();

            return Ok(options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving course code options");
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = Messages.Error.InternalServerError
            });
        }
    }

    /// <summary>
    /// Deletes a course code (Staff only)
    /// </summary>
    /// <param name="id">The course code ID</param>
    /// <returns>Delete confirmation</returns>
    /// <response code="200">Course code deleted successfully</response>
    /// <response code="400">Course code is in use and cannot be deleted</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Staff role required</response>
    /// <response code="404">Course code not found</response>
    /// <response code="500">Internal server error</response>
    [HttpDelete("{id}")]
    [Authorize(Roles = RoleConstants.Staff)]
    [ProducesResponseType(typeof(DeleteCourseCodeResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DeleteCourseCodeResponse>> DeleteCourseCode(Guid id)
    {
        try
        {
            var command = new DeleteCourseCodeCommand { Id = id };
            var response = await _mediator.Send(command);
            
            if (!response.Success)
            {
                if (response.Message.Contains("not found"))
                {
                    return NotFound(response);
                }
                return BadRequest(response);
            }

            _logger.LogInformation("Course code deleted successfully: {Id}", id);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting course code {Id}", id);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = Messages.Error.InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets all courses for a specific course code (All authenticated users)
    /// </summary>
    /// <param name="id">The course code ID</param>
    /// <param name="page">Page number for pagination (1-based)</param>
    /// <param name="pageSize">Number of items per page (max 100)</param>
    /// <param name="sortDirection">Sort direction (asc or desc) for created date</param>
    /// <returns>Paginated list of courses for the course code</returns>
    /// <response code="200">Courses retrieved successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Course code not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id}/courses")]
    [ProducesResponseType(typeof(GetCourseCodeCoursesResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GetCourseCodeCoursesResponse>> GetCourseCodeCourses(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortDirection = "desc")
    {
        try
        {
            var query = new GetCourseCodeCoursesQuery
            {
                CourseCodeId = id,
                Page = page,
                PageSize = pageSize,
                SortDirection = sortDirection
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
            _logger.LogError(ex, "Error retrieving courses for course code {Id}", id);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = Messages.Error.InternalServerError
            });
        }
    }
}