using ClassroomService.Application.Features.CourseRequests.Commands;
using ClassroomService.Application.Features.CourseRequests.DTOs;
using ClassroomService.Application.Features.CourseRequests.Queries;
using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using HttpStatusCodes = Microsoft.AspNetCore.Http.StatusCodes;

namespace ClassroomService.Application.Controllers;

/// <summary>
/// Controller for managing course requests
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CourseRequestsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public CourseRequestsController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Creates a new course request (Lecturer only)
    /// </summary>
    /// <param name="command">Course request details</param>
    /// <returns>Created course request</returns>
    /// <response code="201">Course request created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Lecturers only</response>
    /// <response code="409">Course or request already exists</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(CreateCourseRequestResponse), HttpStatusCodes.Status201Created)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status409Conflict)]
    [ProducesResponseType(HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CreateCourseRequestResponse>> CreateCourseRequest(
        [FromForm] CreateCourseRequestCommand command)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(new { message = "User ID not found" });
        }

        command.LecturerId = _currentUserService.UserId.Value;
        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            if (response.Message.Contains("already exists"))
            {
                return Conflict(response);
            }
            return BadRequest(response);
        }

        return CreatedAtAction(nameof(GetCourseRequest), new { id = response.CourseRequestId }, response);
    }

    /// <summary>
    /// Gets all course requests (Staff only)
    /// </summary>
    /// <param name="status">Filter by status</param>
    /// <param name="lecturerName">Filter by lecturer name</param>
    /// <param name="courseCode">Filter by course code</param>
    /// <param name="term">Filter by term</param>
    /// <param name="department">Filter by department</param>
    /// <param name="createdAfter">Filter requests created after this date</param>
    /// <param name="createdBefore">Filter requests created before this date</param>
    /// <param name="sortBy">Sort field</param>
    /// <param name="sortDirection">Sort direction (asc/desc)</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated list of course requests</returns>
    /// <response code="200">Course requests retrieved successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Staff only</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [Authorize(Roles = RoleConstants.Staff)]
    [ProducesResponseType(typeof(GetAllCourseRequestsResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GetAllCourseRequestsResponse>> GetAllCourseRequests(
        [FromQuery] Domain.Enums.CourseRequestStatus? status = null,
        [FromQuery] string? lecturerName = null,
        [FromQuery] string? courseCode = null,
        [FromQuery] string? term = null,
        [FromQuery] string? department = null,
        [FromQuery] DateTime? createdAfter = null,
        [FromQuery] DateTime? createdBefore = null,
        [FromQuery] string sortBy = "CreatedAt",
        [FromQuery] string sortDirection = "desc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetAllCourseRequestsQuery
        {
            Filter = new CourseRequestFilterDto
            {
                Status = status,
                LecturerName = lecturerName,
                CourseCode = courseCode,
                Term = term,
                Department = department,
                CreatedAfter = createdAfter,
                CreatedBefore = createdBefore,
                SortBy = sortBy,
                SortDirection = sortDirection,
                Page = page,
                PageSize = pageSize
            },
            CurrentUserId = _currentUserService.UserId ?? Guid.Empty,
            CurrentUserRole = _currentUserService.Role ?? ""
        };

        var response = await _mediator.Send(query);
        return Ok(response);
    }

    /// <summary>
    /// Gets lecturer's own course requests
    /// </summary>
    /// <param name="status">Filter by status</param>
    /// <param name="courseCode">Filter by course code</param>
    /// <param name="term">Filter by term</param>
    /// <param name="department">Filter by department</param>
    /// <param name="createdAfter">Filter requests created after this date</param>
    /// <param name="createdBefore">Filter requests created before this date</param>
    /// <param name="sortBy">Sort field</param>
    /// <param name="sortDirection">Sort direction (asc/desc)</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated list of lecturer's course requests</returns>
    /// <response code="200">Course requests retrieved successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Lecturers only</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("my-requests")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(GetMyCourseRequestsResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GetMyCourseRequestsResponse>> GetMyCourseRequests(
        [FromQuery] Domain.Enums.CourseRequestStatus? status = null,
        [FromQuery] string? courseCode = null,
        [FromQuery] string? term = null,
        [FromQuery] string? department = null,
        [FromQuery] DateTime? createdAfter = null,
        [FromQuery] DateTime? createdBefore = null,
        [FromQuery] string sortBy = "CreatedAt",
        [FromQuery] string sortDirection = "desc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(new { message = "User ID not found" });
        }

        var query = new GetMyCourseRequestsQuery
        {
            Filter = new CourseRequestFilterDto
            {
                Status = status,
                CourseCode = courseCode,
                Term = term,
                Department = department,
                CreatedAfter = createdAfter,
                CreatedBefore = createdBefore,
                SortBy = sortBy,
                SortDirection = sortDirection,
                Page = page,
                PageSize = pageSize
            },
            LecturerId = _currentUserService.UserId.Value
        };

        var response = await _mediator.Send(query);
        return Ok(response);
    }

    /// <summary>
    /// Gets a specific course request by ID (Staff/Lecturer)
    /// </summary>
    /// <param name="id">Course request ID</param>
    /// <returns>Course request details</returns>
    /// <response code="200">Course request found</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Not authorized to view this request</response>
    /// <response code="404">Course request not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id}")]
    [Authorize(Roles = $"{RoleConstants.Staff},{RoleConstants.Lecturer}")]
    [ProducesResponseType(typeof(GetCourseRequestResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GetCourseRequestResponse>> GetCourseRequest(Guid id)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(new { message = "User ID not found" });
        }

        var query = new GetCourseRequestQuery
        {
            CourseRequestId = id,
            CurrentUserId = _currentUserService.UserId.Value,
            CurrentUserRole = _currentUserService.Role ?? ""
        };

        var response = await _mediator.Send(query);

        if (!response.Success)
        {
            if (response.Message.Contains("not found"))
            {
                return NotFound(response);
            }
            if (response.Message.Contains("not authorized"))
            {
                return Forbid();
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Processes a course request - approve or reject (Staff only)
    /// </summary>
    /// <param name="id">Course request ID</param>
    /// <param name="command">Processing details</param>
    /// <returns>Processing result</returns>
    /// <response code="200">Course request processed successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Staff only</response>
    /// <response code="404">Course request not found</response>
    /// <response code="409">Course request already processed</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("{id}/process")]
    [Authorize(Roles = RoleConstants.Staff)]
    [ProducesResponseType(typeof(ProcessCourseRequestResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpStatusCodes.Status409Conflict)]
    [ProducesResponseType(HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProcessCourseRequestResponse>> ProcessCourseRequest(
        Guid id, [FromBody] ProcessCourseRequestCommand command)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(new { message = "User ID not found" });
        }

        command.CourseRequestId = id;
        command.ProcessedBy = _currentUserService.UserId.Value;
        
        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            if (response.Message.Contains("not found"))
            {
                return NotFound(response);
            }
            if (response.Message.Contains("already processed") || response.Message.Contains("already exists"))
            {
                return Conflict(response);
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Uploads a syllabus file for a course request (Lecturer only)
    /// </summary>
    /// <param name="courseRequestId">Course request ID</param>
    /// <param name="file">Syllabus file to upload</param>
    /// <returns>Upload result with file URL</returns>
    /// <response code="200">Syllabus uploaded successfully</response>
    /// <response code="400">Invalid file or request data</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Not the course request owner</response>
    /// <response code="404">Course request not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("{courseRequestId}/syllabus/upload")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [RequestFormLimits(MultipartBodyLengthLimit = 52428800)] // 50MB
    [RequestSizeLimit(52428800)] // 50MB
    [ProducesResponseType(typeof(UploadCourseRequestSyllabusResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UploadCourseRequestSyllabusResponse>> UploadCourseRequestSyllabus(
        Guid courseRequestId, IFormFile file)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(new { message = "User ID not found" });
        }

        var command = new UploadCourseRequestSyllabusCommand
        {
            CourseRequestId = courseRequestId,
            File = file
        };

        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            if (response.Message.Contains("not found"))
            {
                return NotFound(response);
            }
            if (response.Message.Contains("not authorized") || response.Message.Contains("not the lecturer"))
            {
                return Forbid();
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Deletes the syllabus file from a course request (Lecturer only)
    /// </summary>
    /// <param name="courseRequestId">Course request ID</param>
    /// <returns>Deletion result</returns>
    /// <response code="200">Syllabus deleted successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Not the course request owner</response>
    /// <response code="404">Course request not found or no syllabus file</response>
    /// <response code="500">Internal server error</response>
    [HttpDelete("{courseRequestId}/syllabus")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(DeleteCourseRequestSyllabusResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DeleteCourseRequestSyllabusResponse>> DeleteCourseRequestSyllabus(
        Guid courseRequestId)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(new { message = "User ID not found" });
        }

        var command = new DeleteCourseRequestSyllabusCommand
        {
            CourseRequestId = courseRequestId
        };

        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            if (response.Message.Contains("not found"))
            {
                return NotFound(response);
            }
            if (response.Message.Contains("not authorized") || response.Message.Contains("not the lecturer"))
            {
                return Forbid();
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Updates a course request (Lecturer only, Pending status only)
    /// </summary>
    /// <param name="id">Course request ID</param>
    /// <param name="command">Update details</param>
    /// <returns>Update result</returns>
    /// <response code="200">Course request updated successfully</response>
    /// <response code="400">Invalid request data or not in pending status</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Not the course request owner</response>
    /// <response code="404">Course request not found</response>
    /// <response code="409">Duplicate course request exists</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("{id}")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(UpdateCourseRequestResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpStatusCodes.Status409Conflict)]
    [ProducesResponseType(HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UpdateCourseRequestResponse>> UpdateCourseRequest(
        Guid id, [FromBody] UpdateCourseRequestCommand command)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(new { message = "User ID not found" });
        }

        command.CourseRequestId = id;
        command.LecturerId = _currentUserService.UserId.Value;

        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            if (response.Message.Contains("not found"))
            {
                return NotFound(response);
            }
            if (response.Message.Contains("not authorized"))
            {
                return Forbid();
            }
            if (response.Message.Contains("already exists"))
            {
                return Conflict(response);
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Deletes a course request (Lecturer only, Pending status only)
    /// </summary>
    /// <param name="id">Course request ID</param>
    /// <returns>Deletion result</returns>
    /// <response code="200">Course request deleted successfully</response>
    /// <response code="400">Invalid request or not in pending status</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Not the course request owner</response>
    /// <response code="404">Course request not found</response>
    /// <response code="500">Internal server error</response>
    [HttpDelete("{id}")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(DeleteCourseRequestResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DeleteCourseRequestResponse>> DeleteCourseRequest(Guid id)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(new { message = "User ID not found" });
        }

        var command = new DeleteCourseRequestCommand
        {
            CourseRequestId = id,
            LecturerId = _currentUserService.UserId.Value
        };

        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            if (response.Message.Contains("not found"))
            {
                return NotFound(response);
            }
            if (response.Message.Contains("not authorized"))
            {
                return Forbid();
            }
            return BadRequest(response);
        }

        return Ok(response);
    }
}