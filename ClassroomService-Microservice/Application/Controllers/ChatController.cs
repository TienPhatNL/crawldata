using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Application.Features.Chat.Commands;
using ClassroomService.Application.Features.Chat.Queries;
using ClassroomService.Application.Hubs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace ClassroomService.Application.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ChatController> _logger;
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatController(
        IMediator mediator,
        ICurrentUserService currentUserService,
        ILogger<ChatController> logger,
        IHubContext<ChatHub> hubContext)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _logger = logger;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Get my conversations (optionally filtered by course)
    /// </summary>
    [HttpGet("conversations")]
    [ProducesResponseType(typeof(GetMyConversationsResponse), 200)]
    public async Task<ActionResult<GetMyConversationsResponse>> GetMyConversations(
        [FromQuery] Guid? courseId = null)
    {
        var userId = _currentUserService.UserId!.Value;
        
        var query = new GetMyConversationsQuery
        {
            UserId = userId,
            CourseId = courseId
        };

        var response = await _mediator.Send(query);
        
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Get messages in a conversation (paginated)
    /// </summary>
    [HttpGet("conversations/{conversationId}/messages")]
    [ProducesResponseType(typeof(GetConversationMessagesResponse), 200)]
    public async Task<ActionResult<GetConversationMessagesResponse>> GetMessages(
        Guid conversationId,
        [FromQuery] Guid? supportRequestId = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        var userId = _currentUserService.UserId!.Value;
        
        var query = new GetConversationMessagesQuery
        {
            ConversationId = conversationId,
            UserId = userId,
            SupportRequestId = supportRequestId,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var response = await _mediator.Send(query);
        
        if (!response.Success)
        {
            if (response.Message.Contains("not found"))
            {
                return NotFound(response);
            }
            if (response.Message.Contains("denied"))
            {
                return Forbid();
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Get users I can chat with in a course, ordered by latest message
    /// </summary>
    [HttpGet("courses/{courseId}/users")]
    [ProducesResponseType(typeof(GetCourseUsersResponse), 200)]
    public async Task<ActionResult<GetCourseUsersResponse>> GetCourseUsers(Guid courseId)
    {
        var userId = _currentUserService.UserId!.Value;
        var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty;
        
        var query = new GetCourseUsersQuery
        {
            CourseId = courseId,
            UserId = userId,
            UserRole = userRole
        };

        var response = await _mediator.Send(query);
        
        if (!response.Success)
        {
            if (response.Message.Contains("denied"))
            {
                return Forbid();
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Delete my message (soft delete)
    /// </summary>
    [HttpDelete("messages/{messageId}")]
    [ProducesResponseType(typeof(DeleteMessageResponse), 200)]
    [ProducesResponseType(204)]
    public async Task<ActionResult<DeleteMessageResponse>> DeleteMessage(Guid messageId)
    {
        var userId = _currentUserService.UserId!.Value;
        
        var command = new DeleteMessageCommand
        {
            MessageId = messageId,
            UserId = userId
        };

        var response = await _mediator.Send(command);
        
        if (!response.Success)
        {
            if (response.Message.Contains("not found"))
            {
                return NotFound(response);
            }
            if (response.Message.Contains("only delete"))
            {
                return Forbid();
            }
            return BadRequest(response);
        }

        // Broadcast deletion to the receiver via SignalR
        if (response.ReceiverId.HasValue)
        {
            await _hubContext.Clients.User(response.ReceiverId.Value.ToString())
                .SendAsync("MessageDeleted", new { MessageId = messageId });
        }

        return NoContent();
    }

    /// <summary>
    /// Upload a CSV file to a conversation
    /// </summary>
    /// <param name="conversationId">The conversation ID</param>
    /// <param name="file">The CSV file to upload</param>
    /// <returns>The upload response with file metadata</returns>
    /// <response code="200">File uploaded successfully</response>
    /// <response code="400">Invalid file or request error</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - No access to conversation</response>
    /// <response code="404">Conversation not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("conversations/{conversationId}/upload-csv")]
    [ProducesResponseType(typeof(UploadConversationCsvResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [RequestFormLimits(MultipartBodyLengthLimit = 10485760)] // 10MB
    [RequestSizeLimit(10485760)] // 10MB
    public async Task<ActionResult<UploadConversationCsvResponse>> UploadConversationCsv(
        Guid conversationId,
        IFormFile file)
    {
        try
        {
            var command = new UploadConversationCsvCommand
            {
                ConversationId = conversationId,
                File = file
            };

            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                if (response.Message.Contains("not found"))
                {
                    return NotFound(response);
                }
                if (response.Message.Contains("Access denied") || response.Message.Contains("denied"))
                {
                    return StatusCode(403, response);
                }
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading CSV file to conversation {ConversationId}", conversationId);
            return StatusCode(500, new UploadConversationCsvResponse
            {
                Success = false,
                Message = "Internal server error"
            });
        }
    }

    /// <summary>
    /// Get uploaded files for a conversation
    /// </summary>
    [HttpGet("conversations/{conversationId}/files")]
    [ProducesResponseType(typeof(GetConversationUploadedFilesResponse), 200)]
    public async Task<ActionResult<GetConversationUploadedFilesResponse>> GetConversationUploadedFiles(
        Guid conversationId)
    {
        var userId = _currentUserService.UserId!.Value;

        var query = new GetConversationUploadedFilesQuery
        {
            ConversationId = conversationId,
            UserId = userId
        };

        var response = await _mediator.Send(query);

        if (!response.Success)
        {
            if (response.Message.Contains("not found"))
            {
                return NotFound(response);
            }
            if (response.Message.Contains("denied"))
            {
                return Forbid();
            }
            return BadRequest(response);
        }

        return Ok(response);
    }
}
