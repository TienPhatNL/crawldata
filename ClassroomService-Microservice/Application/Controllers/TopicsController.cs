using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClassroomService.Application.Features.Topics.Commands;
using ClassroomService.Application.Features.Topics.Queries;
using ClassroomService.Domain.Constants;
using ClassroomService.Application.Common.Interfaces;
using HttpStatusCodes = Microsoft.AspNetCore.Http.StatusCodes;

namespace ClassroomService.Application.Controllers;

/// <summary>
/// Controller for managing assignment topics/categories
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
[Tags("Topics")]
public class TopicsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public TopicsController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Creates a new topic (Staff only)
    /// </summary>
    /// <param name="command">The topic creation details</param>
    /// <returns>The topic creation response</returns>
    /// <response code="200">Topic created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Staff role required</response>
    /// <response code="409">Topic with same name already exists</response>
    [HttpPost]
    [Authorize(Roles = RoleConstants.Staff)]
    [ProducesResponseType(typeof(CreateTopicResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateTopicResponse>> CreateTopic([FromBody] CreateTopicCommand command)
    {
        var response = await _mediator.Send(command);
        
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Updates an existing topic (Staff only)
    /// </summary>
    /// <param name="id">Topic ID</param>
    /// <param name="command">The topic update details</param>
    /// <returns>The topic update response</returns>
    /// <response code="200">Topic updated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Staff role required</response>
    /// <response code="404">Topic not found</response>
    /// <response code="409">Topic with same name already exists</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = RoleConstants.Staff)]
    [ProducesResponseType(typeof(UpdateTopicResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpStatusCodes.Status409Conflict)]
    public async Task<ActionResult<UpdateTopicResponse>> UpdateTopic(Guid id, [FromBody] UpdateTopicCommand command)
    {
        command.Id = id;

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
    /// Deletes a topic (Staff only)
    /// </summary>
    /// <param name="id">Topic ID</param>
    /// <returns>The delete response</returns>
    /// <response code="200">Topic deleted successfully</response>
    /// <response code="400">Cannot delete topic with assignments</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Staff role required</response>
    /// <response code="404">Topic not found</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = RoleConstants.Staff)]
    [ProducesResponseType(typeof(DeleteTopicResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeleteTopicResponse>> DeleteTopic(Guid id)
    {
        var command = new DeleteTopicCommand { Id = id };
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
    /// Get all topics with optional filtering and pagination
    /// </summary>
    /// <param name="query">Query parameters for filtering and pagination</param>
    /// <returns>Paginated list of topics</returns>
    /// <response code="200">Topics retrieved successfully</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet]
    [ProducesResponseType(typeof(GetAllTopicsResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GetAllTopicsResponse>> GetAllTopics([FromQuery] GetAllTopicsQuery query)
    {
        var response = await _mediator.Send(query);
        return Ok(response);
    }

    /// <summary>
    /// Get a specific topic by ID
    /// </summary>
    /// <param name="id">Topic ID</param>
    /// <returns>Topic details</returns>
    /// <response code="200">Topic retrieved successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Topic not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GetTopicByIdResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetTopicByIdResponse>> GetTopicById(Guid id)
    {
        var query = new GetTopicByIdQuery { Id = id };
        var response = await _mediator.Send(query);
        
        if (!response.Success)
        {
            return NotFound(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Get active topics for dropdown selection
    /// </summary>
    /// <returns>List of active topics (ID and Name only)</returns>
    /// <response code="200">Topics retrieved successfully</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("dropdown")]
    [ProducesResponseType(typeof(GetTopicsDropdownResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GetTopicsDropdownResponse>> GetTopicsDropdown()
    {
        var query = new GetTopicsDropdownQuery();
        var response = await _mediator.Send(query);
        return Ok(response);
    }
}
