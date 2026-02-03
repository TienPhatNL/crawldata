using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Application.Features.ReportCollaboration.Commands;
using ClassroomService.Application.Features.ReportCollaboration.Queries;
using ClassroomService.Domain.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClassroomService.Application.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ReportCollaborationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ReportCollaborationController> _logger;

    public ReportCollaborationController(
        IMediator mediator,
        ICurrentUserService currentUserService,
        ILogger<ReportCollaborationController> logger)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Get collaboration session information for a report
    /// </summary>
    [HttpGet("session/{reportId}")]
    [ProducesResponseType(typeof(GetSessionInfoResponse), 200)]
    public async Task<ActionResult<GetSessionInfoResponse>> GetSession(Guid reportId)
    {
        var query = new GetSessionInfoQuery { ReportId = reportId };
        var response = await _mediator.Send(query);

        if (!response.Success)
        {
            return NotFound(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Get list of active collaborators in a report
    /// </summary>
    [HttpGet("{reportId}/collaborators")]
    [ProducesResponseType(typeof(GetActiveCollaboratorsResponse), 200)]
    public async Task<ActionResult<GetActiveCollaboratorsResponse>> GetActiveCollaborators(Guid reportId)
    {
        var query = new GetActiveCollaboratorsQuery { ReportId = reportId };
        var response = await _mediator.Send(query);

        return Ok(response);
    }

    /// <summary>
    /// Force save - create version immediately (Students only, Draft status only)
    /// </summary>
    [HttpPost("{reportId}/force-save")]
    [ProducesResponseType(typeof(ManualSaveResponse), 200)]
    public async Task<ActionResult<ManualSaveResponse>> ForceSave(Guid reportId)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var userRole = _currentUserService.Role;
        
        var command = new ForceSaveCommand
        {
            ReportId = reportId,
            UserId = userId.Value,
            UserRole = userRole
        };

        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            if (response.Message.Contains("not found"))
            {
                return NotFound(response);
            }
            if (response.Message.Contains("Only students"))
            {
                return Forbid();
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Check if there are unsaved changes
    /// </summary>
    [HttpGet("{reportId}/has-unsaved-changes")]
    [ProducesResponseType(typeof(HasUnsavedChangesResponse), 200)]
    public async Task<ActionResult<HasUnsavedChangesResponse>> HasUnsavedChanges(Guid reportId)
    {
        var query = new HasUnsavedChangesQuery { ReportId = reportId };
        var response = await _mediator.Send(query);

        return Ok(response);
    }

    /// <summary>
    /// Get pending change count
    /// </summary>
    [HttpGet("{reportId}/pending-changes-count")]
    [ProducesResponseType(typeof(GetPendingChangesCountResponse), 200)]
    public async Task<ActionResult<GetPendingChangesCountResponse>> GetPendingChangesCount(Guid reportId)
    {
        var query = new GetPendingChangesCountQuery { ReportId = reportId };
        var response = await _mediator.Send(query);

        return Ok(response);
    }
}