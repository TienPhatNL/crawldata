using ClassroomService.Application.Features.CrawlerChat.Queries;
using ClassroomService.Domain.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClassroomService.Application.Controllers;

/// <summary>
/// Controller for crawler chat message operations
/// </summary>
[ApiController]
[Route("api/crawler-chat")]
[Authorize]
public class CrawlerChatController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<CrawlerChatController> _logger;

    public CrawlerChatController(
        IMediator mediator,
        ILogger<CrawlerChatController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all messages for a specific assignment
    /// </summary>
    /// <param name="assignmentId">Assignment ID</param>
    /// <param name="limit">Maximum number of messages to return</param>
    /// <param name="offset">Number of messages to skip</param>
    /// <returns>List of crawler chat messages</returns>
    [HttpGet("assignment/{assignmentId}/messages")]
    [ProducesResponseType(typeof(List<CrawlerChatMessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<CrawlerChatMessageDto>>> GetAssignmentMessages(
        Guid assignmentId,
        [FromQuery] int limit = 100,
        [FromQuery] int offset = 0)
    {
        try
        {
            _logger.LogInformation("API request to get messages for assignment {AssignmentId} (limit: {Limit}, offset: {Offset})",
                assignmentId, limit, offset);

            var query = new GetAssignmentMessagesQuery
            {
                AssignmentId = assignmentId,
                Limit = limit,
                Offset = offset
            };

            var messages = await _mediator.Send(query);

            return Ok(messages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving messages for assignment {AssignmentId}", assignmentId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving messages" });
        }
    }

    /// <summary>
    /// Get all messages for a specific conversation
    /// </summary>
    /// <param name="conversationId">Conversation ID</param>
    /// <param name="limit">Maximum number of messages to return</param>
    /// <param name="offset">Number of messages to skip</param>
    /// <returns>List of crawler chat messages</returns>
    [HttpGet("conversation/{conversationId}/messages")]
    [ProducesResponseType(typeof(List<CrawlerChatMessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<CrawlerChatMessageDto>>> GetConversationMessages(
        Guid conversationId,
        [FromQuery] int limit = 100,
        [FromQuery] int offset = 0)
    {
        try
        {
            _logger.LogInformation("API request to get messages for conversation {ConversationId} (limit: {Limit}, offset: {Offset})",
                conversationId, limit, offset);

            var query = new GetConversationMessagesQuery
            {
                ConversationId = conversationId,
                Limit = limit,
                Offset = offset
            };

            var messages = await _mediator.Send(query);

            return Ok(messages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving messages for conversation {ConversationId}", conversationId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving messages" });
        }
    }

    /// <summary>
    /// Get all conversations for a specific assignment
    /// </summary>
    /// <param name="assignmentId">Assignment ID</param>
    /// <param name="myOnly">If true, only return conversations the current user participated in</param>
    /// <returns>List of conversation summaries</returns>
    [HttpGet("assignment/{assignmentId}/conversations")]
    [ProducesResponseType(typeof(List<ConversationSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ConversationSummaryDto>>> GetAssignmentConversations(
        Guid assignmentId,
        [FromQuery] bool myOnly = false)
    {
        try
        {
            var userId = myOnly ? GetCurrentUserId() : (Guid?)null;

            _logger.LogInformation("API request to get conversations for assignment {AssignmentId} (myOnly: {MyOnly}, userId: {UserId})",
                assignmentId, myOnly, userId);

            var query = new GetAssignmentConversationsQuery
            {
                AssignmentId = assignmentId,
                UserId = userId
            };

            var conversations = await _mediator.Send(query);

            return Ok(conversations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving conversations for assignment {AssignmentId}", assignmentId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving conversations" });
        }
    }

    /// <summary>
    /// Get the current authenticated user's ID from claims
    /// </summary>
    /// <returns>User ID</returns>
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value
                          ?? User.FindFirst("userId")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
