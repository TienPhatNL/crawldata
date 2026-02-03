using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClassroomService.Application.Features.Terms.Commands;
using ClassroomService.Application.Features.Terms.Queries;
using ClassroomService.Domain.Constants;
using ClassroomService.Application.Common.Interfaces;
using HttpStatusCodes = Microsoft.AspNetCore.Http.StatusCodes;

namespace ClassroomService.Application.Controllers;

/// <summary>
/// Controller for managing academic terms
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Terms")]
public class TermsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public TermsController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Creates a new term (Staff only)
    /// </summary>
    /// <param name="command">The term creation details</param>
    /// <returns>The term creation response</returns>
    /// <response code="200">Term created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Staff role required</response>
    /// <response code="409">Term with same name already exists</response>
    [HttpPost]
    [Authorize(Roles = RoleConstants.Staff)]
    [ProducesResponseType(typeof(CreateTermResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateTermResponse>> CreateTerm([FromBody] CreateTermCommand command)
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

        return Ok(response);
    }

    /// <summary>
    /// Updates an existing term (Staff only)
    /// </summary>
    /// <param name="id">The term ID</param>
    /// <param name="command">The term update details</param>
    /// <returns>The term update response</returns>
    /// <response code="200">Term updated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Staff role required</response>
    /// <response code="404">Term not found</response>
    /// <response code="409">Term name already exists or cannot deactivate</response>
    [HttpPut("{id}")]
    [Authorize(Roles = RoleConstants.Staff)]
    [ProducesResponseType(typeof(UpdateTermResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpStatusCodes.Status409Conflict)]
    public async Task<ActionResult<UpdateTermResponse>> UpdateTerm(Guid id, [FromBody] UpdateTermCommand command)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(new { message = Messages.Error.UserIdNotFound });
        }

        command.Id = id;
        command.UpdatedBy = _currentUserService.UserId.Value;

        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            if (response.Message.Contains("not found"))
            {
                return NotFound(response);
            }
            if (response.Message.Contains("already exists") || response.Message.Contains("Cannot deactivate"))
            {
                return Conflict(response);
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Gets all terms with optional filtering and pagination (Public access)
    /// </summary>
    /// <param name="activeOnly">Filter to only active terms</param>
    /// <param name="name">Filter by term name (partial match, case-insensitive)</param>
    /// <param name="page">Page number (starting from 1)</param>
    /// <param name="pageSize">Number of items per page (max 100)</param>
    /// <param name="sortBy">Sort field (Name, CreatedAt, UpdatedAt)</param>
    /// <param name="sortDirection">Sort direction (asc or desc)</param>
    /// <returns>Paginated list of terms</returns>
    /// <response code="200">Terms retrieved successfully</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GetAllTermsResponse), HttpStatusCodes.Status200OK)]
    public async Task<ActionResult<GetAllTermsResponse>> GetAllTerms(
        [FromQuery] bool? activeOnly = null,
        [FromQuery] string? name = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "Name",
        [FromQuery] string sortDirection = "asc")
    {
        var query = new GetAllTermsQuery
        {
            ActiveOnly = activeOnly,
            Name = name,
            Page = page,
            PageSize = pageSize,
            SortBy = sortBy,
            SortDirection = sortDirection
        };

        var response = await _mediator.Send(query);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Gets active terms for dropdown selection (Public access)
    /// </summary>
    /// <returns>List of active terms (Id and Name only)</returns>
    /// <response code="200">Active terms retrieved successfully</response>
    [HttpGet("dropdown")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GetTermsDropdownResponse), HttpStatusCodes.Status200OK)]
    public async Task<ActionResult<GetTermsDropdownResponse>> GetTermsDropdown()
    {
        var query = new GetTermsDropdownQuery();
        var response = await _mediator.Send(query);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Gets a term by ID (Public access)
    /// </summary>
    /// <param name="id">The term ID</param>
    /// <returns>The term details</returns>
    /// <response code="200">Term retrieved successfully</response>
    /// <response code="404">Term not found</response>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GetTermByIdResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetTermByIdResponse>> GetTermById(Guid id)
    {
        var query = new GetTermByIdQuery { Id = id };
        var response = await _mediator.Send(query);

        if (!response.Success)
        {
            return NotFound(response);
        }

        return Ok(response);
    }
}
