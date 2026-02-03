using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Application.Features.SupportRequests.Commands;
using ClassroomService.Application.Features.SupportRequests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClassroomService.Application.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SupportRequestsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<SupportRequestsController> _logger;

    public SupportRequestsController(
        IMediator mediator,
        ICurrentUserService currentUserService,
        ILogger<SupportRequestsController> logger)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new support request (Lecturer or Student only)
    /// Valid status transitions:
    /// - Pending: Initial status when created
    /// - InProgress: When staff accepts the request
    /// - Resolved: When requester marks it as resolved
    /// - Cancelled: When requester cancels before staff accepts
    /// - Rejected: When staff rejects the request
    /// Supports optional image attachments during creation
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Student,Lecturer")]
    [ProducesResponseType(typeof(CreateSupportRequestResponse), 200)]
    public async Task<ActionResult<CreateSupportRequestResponse>> CreateSupportRequest(
        [FromForm] Guid courseId,
        [FromForm] int priority,
        [FromForm] int category,
        [FromForm] string subject,
        [FromForm] string description,
        [FromForm] List<IFormFile>? images)
    {
        var userId = _currentUserService.UserId!.Value;

        var command = new CreateSupportRequestCommand
        {
            CourseId = courseId,
            RequesterId = userId,
            Priority = priority,
            Category = category,
            Subject = subject,
            Description = description,
            Images = images
        };

        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Get my support requests (Students/Lecturers see their own requests)
    /// </summary>
    [HttpGet("my")]
    [Authorize(Roles = "Student,Lecturer")]
    [ProducesResponseType(typeof(GetMySupportRequestsResponse), 200)]
    public async Task<ActionResult<GetMySupportRequestsResponse>> GetMySupportRequests(
        [FromQuery] Guid? courseId = null,
        [FromQuery] string? status = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = _currentUserService.UserId!.Value;

        var query = new GetMySupportRequestsQuery
        {
            UserId = userId,
            CourseId = courseId,
            Status = status,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var response = await _mediator.Send(query);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Get a support request by ID
    /// Staff can view any request, Students/Lecturers can only view their own or assigned requests
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Student,Lecturer,Staff")]
    [ProducesResponseType(typeof(GetSupportRequestByIdResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<GetSupportRequestByIdResponse>> GetSupportRequestById(Guid id)
    {
        var userId = _currentUserService.UserId!.Value;
        var userRole = _currentUserService.Role ?? string.Empty;

        var query = new GetSupportRequestByIdQuery
        {
            SupportRequestId = id,
            UserId = userId,
            UserRole = userRole
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
                return StatusCode(403, response);
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Get all pending support requests (Staff only)
    /// </summary>
    [HttpGet("pending")]
    [Authorize(Roles = "Staff")]
    [ProducesResponseType(typeof(GetPendingSupportRequestsResponse), 200)]
    public async Task<ActionResult<GetPendingSupportRequestsResponse>> GetPendingSupportRequests(
        [FromQuery] Guid? courseId = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetPendingSupportRequestsQuery
        {
            CourseId = courseId,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var response = await _mediator.Send(query);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Accept a support request (Staff only)
    /// Transitions status from Pending → InProgress
    /// Creates a conversation between staff and requester
    /// </summary>
    [HttpPost("{id}/accept")]
    [Authorize(Roles = "Staff")]
    [ProducesResponseType(typeof(AcceptSupportRequestResponse), 200)]
    public async Task<ActionResult<AcceptSupportRequestResponse>> AcceptSupportRequest(Guid id)
    {
        var staffId = _currentUserService.UserId!.Value;

        var command = new AcceptSupportRequestCommand
        {
            SupportRequestId = id,
            StaffId = staffId
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

        return Ok(response);
    }

    /// <summary>
    /// Get staff's assigned support requests (Staff only)
    /// </summary>
    [HttpGet("assigned")]
    [Authorize(Roles = "Staff")]
    [ProducesResponseType(typeof(GetStaffSupportRequestsResponse), 200)]
    public async Task<ActionResult<GetStaffSupportRequestsResponse>> GetAssignedSupportRequests(
        [FromQuery] string? status = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var staffId = _currentUserService.UserId!.Value;

        var query = new GetStaffSupportRequestsQuery
        {
            StaffId = staffId,
            Status = status,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var response = await _mediator.Send(query);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Cancel a support request (Requester only)
    /// Transitions status from Pending → Cancelled
    /// Can only cancel requests that haven't been accepted yet
    /// </summary>
    [HttpPatch("{id}")]
    [Authorize(Roles = "Student,Lecturer")]
    [ProducesResponseType(typeof(CancelSupportRequestResponse), 200)]
    public async Task<ActionResult<CancelSupportRequestResponse>> CancelSupportRequest(Guid id)
    {
        var userId = _currentUserService.UserId!.Value;

        var command = new CancelSupportRequestCommand
        {
            SupportRequestId = id,
            UserId = userId
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

        return Ok(response);
    }

    /// <summary>
    /// Reject a support request (Staff only)
    /// Transitions status from Pending → Rejected
    /// Requires rejection reason and optional comments
    /// </summary>
    [HttpPost("{id}/reject")]
    [Authorize(Roles = "Staff")]
    [ProducesResponseType(typeof(RejectSupportRequestResponse), 200)]
    public async Task<ActionResult<RejectSupportRequestResponse>> RejectSupportRequest(Guid id, [FromBody] RejectSupportRequestCommand command)
    {
        var staffId = _currentUserService.UserId!.Value;

        command.SupportRequestId = id;
        command.StaffId = staffId;

        var response = await _mediator.Send(command);

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

    /// <summary>
    /// Resolve a support request (Requester only)
    /// Transitions status from InProgress → Resolved
    /// Closes the conversation, preventing further messages
    /// </summary>
    [HttpPost("{id}/resolve")]
    [Authorize(Roles = "Student,Lecturer")]
    [ProducesResponseType(typeof(ResolveSupportRequestResponse), 200)]
    public async Task<ActionResult<ResolveSupportRequestResponse>> ResolveSupportRequest(Guid id)
    {
        var userId = _currentUserService.UserId!.Value;

        var command = new ResolveSupportRequestCommand
        {
            SupportRequestId = id,
            UserId = userId
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

        return Ok(response);
    }

    /// <summary>
    /// Upload image attachments to a support request (Requester only)
    /// Supports multiple image uploads in a single request
    /// Images are stored as a JSON array of URLs
    /// </summary>
    [HttpPost("{id}/upload-images")]
    [Authorize(Roles = "Student,Lecturer")]
    [ProducesResponseType(typeof(UploadSupportRequestImagesResponse), 200)]
    public async Task<ActionResult<UploadSupportRequestImagesResponse>> UploadSupportRequestImages(
        Guid id,
        [FromForm] List<IFormFile> images)
    {
        var command = new UploadSupportRequestImagesCommand
        {
            SupportRequestId = id,
            Images = images
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

        return Ok(response);
    }

    /// <summary>
    /// Remove a specific image from a support request (Requester only)
    /// Deletes the image from storage and removes the URL from the JSON array
    /// </summary>
    [HttpDelete("{id}/remove-image")]
    [Authorize(Roles = "Student,Lecturer")]
    [ProducesResponseType(typeof(RemoveSupportRequestImageResponse), 200)]
    public async Task<ActionResult<RemoveSupportRequestImageResponse>> RemoveSupportRequestImage(
        Guid id,
        [FromBody] RemoveSupportRequestImageCommand command)
    {
        command.SupportRequestId = id;

        var response = await _mediator.Send(command);

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
}
