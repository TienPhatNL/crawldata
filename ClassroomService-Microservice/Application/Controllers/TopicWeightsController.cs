using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ClassroomService.Application.Features.TopicWeights.Commands;
using ClassroomService.Application.Features.TopicWeights.Queries;
using ClassroomService.Application.Features.TopicWeights.DTOs;
using ClassroomService.Application.Features.Topics.Queries;
using ClassroomService.Application.Features.Topics.DTOs;
using ClassroomService.Domain.Common;

namespace ClassroomService.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TopicWeightsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TopicWeightsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all TopicWeights across all courses with filtering and pagination (Staff only)
    /// </summary>
    /// <param name="canEdit">Filter to show only weights that can be edited (not in active terms)</param>
    [HttpGet]
    [Authorize(Roles = "Staff")]
    public async Task<ActionResult<PagedResult<TopicWeightResponseDto>>> GetAllTopicWeights(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? courseCode = null,
        [FromQuery] string? topicName = null,
        [FromQuery] string? courseName = null,
        [FromQuery] Guid? courseCodeId = null,
        [FromQuery] Guid? specificCourseId = null,
        [FromQuery] Guid? topicId = null,
        [FromQuery] bool? canEdit = null)
    {
        var query = new GetAllTopicWeightsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            CourseCode = courseCode,
            TopicName = topicName,
            CourseName = courseName,
            CourseCodeId = courseCodeId,
            SpecificCourseId = specificCourseId,
            TopicId = topicId,
            CanEdit = canEdit
        };
        
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get a specific TopicWeight by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Staff,Lecturer")]
    public async Task<ActionResult<TopicWeightResponseDto>> GetTopicWeightById(Guid id)
    {
        var query = new GetTopicWeightByIdQuery { Id = id };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get all configured weights for a specific CourseCode
    /// </summary>
    [HttpGet("coursecode/{courseCodeId:guid}")]
    [Authorize(Roles = "Staff,Lecturer")]
    public async Task<ActionResult<List<TopicWeightResponseDto>>> GetTopicWeightsForCourseCode(Guid courseCodeId)
    {
        var query = new GetTopicWeightsForCourseCodeQuery { CourseCodeId = courseCodeId };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get all configured weights for a specific Course (includes course-specific and course code weights)
    /// </summary>
    [HttpGet("course/{courseId:guid}")]
    [Authorize(Roles = "Staff,Lecturer")]
    public async Task<ActionResult<List<TopicWeightResponseDto>>> GetTopicWeightsForCourse(Guid courseId)
    {
        try
        {
            var query = new GetTopicWeightsForCourseQuery { CourseId = courseId };
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get available topics (with configured weights) for a specific course
    /// </summary>
    [HttpGet("course/{courseId:guid}/available")]
    [Authorize(Roles = "Staff,Lecturer")]
    public async Task<ActionResult<List<TopicWithWeightDto>>> GetAvailableTopicsForCourse(Guid courseId)
    {
        var query = new GetAvailableTopicsForCourseQuery { CourseId = courseId };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Create a new TopicWeight configuration
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Staff")]
    public async Task<ActionResult<TopicWeightResponseDto>> CreateTopicWeight([FromBody] CreateTopicWeightDto dto)
    {
        try
        {
            var userId = GetUserId();
            
            var command = new CreateTopicWeightCommand
            {
                TopicId = dto.TopicId,
                CourseCodeId = dto.CourseCodeId,
                SpecificCourseId = dto.SpecificCourseId,
                WeightPercentage = dto.WeightPercentage,
                Description = dto.Description,
                ConfiguredBy = userId
            };

            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetTopicWeightById), new { id = result.Id }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An unexpected error occurred", details = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing TopicWeight configuration
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Staff")]
    public async Task<ActionResult<TopicWeightResponseDto>> UpdateTopicWeight(Guid id, [FromBody] UpdateTopicWeightDto dto)
    {
        try
        {
            var userId = GetUserId();
            
            var command = new UpdateTopicWeightCommand
            {
                Id = id,
                WeightPercentage = dto.WeightPercentage,
                Description = dto.Description,
                ConfiguredBy = userId
            };

            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a TopicWeight configuration
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Staff")]
    public async Task<ActionResult<DeleteTopicWeightResponse>> DeleteTopicWeight(Guid id)
    {
        var command = new DeleteTopicWeightCommand { Id = id };
        var result = await _mediator.Send(command);
        
        if (!result.Success)
        {
            if (result.Message.Contains("not found"))
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }
        
        return Ok(result);
    }

    /// <summary>
    /// Bulk configure TopicWeights for a CourseCode (replaces all existing configurations)
    /// </summary>
    [HttpPost("coursecode/{courseCodeId:guid}/bulk")]
    [Authorize(Roles = "Staff")]
    public async Task<ActionResult<List<TopicWeightResponseDto>>> BulkConfigureTopicWeights(
        Guid courseCodeId, 
        [FromBody] List<TopicWeightConfigDto> topicWeights)
    {
        try
        {
            var userId = GetUserId();
            
            var command = new BulkConfigureTopicWeightsCommand
            {
                CourseCodeId = courseCodeId,
                TopicWeights = topicWeights,
                ConfiguredBy = userId
            };

            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Bulk create multiple topic weights for a specific course (Staff only)
    /// Special rule: Allowed even during active terms if course status is PendingApproval
    /// Total must be exactly 100%. Blocks if course has any assignments.
    /// </summary>
    [HttpPost("course/{courseId:guid}/bulk")]
    [Authorize(Roles = "Staff")]
    public async Task<ActionResult<BulkCreateCourseTopicWeightsResponse>> BulkCreateCourseTopicWeights(
        Guid courseId,
        [FromBody] BulkCreateCourseTopicWeightsCommand command)
    {
        try
        {
            var userId = GetUserId();
            command.ConfiguredBy = userId;
            command.CourseId = courseId; // Enforce CourseId from URL

            var result = await _mediator.Send(command);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while creating topic weights", details = ex.Message });
        }
    }

    /// <summary>
    /// Bulk configure (create or update) multiple topic weights for a specific course (Staff only)
    /// Special rule: Allowed even during active terms if course status is PendingApproval
    /// Creates new weights if they don't exist, updates if they do. Total must be exactly 100%. Blocks if course has any assignments.
    /// </summary>
    [HttpPut("course/{courseId:guid}/bulk")]
    [Authorize(Roles = "Staff")]
    public async Task<ActionResult<BulkUpdateCourseTopicWeightsResponse>> BulkUpdateCourseTopicWeights(
        Guid courseId,
        [FromBody] BulkUpdateCourseTopicWeightsCommand command)
    {
        try
        {
            var userId = GetUserId();
            command.ConfiguredBy = userId;
            command.CourseId = courseId; // Enforce CourseId from URL

            var result = await _mediator.Send(command);
            
            if (!result.Success && result.SuccessCount == 0)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while updating topic weights", details = ex.Message });
        }
    }

    /// <summary>
    /// Bulk configure (create or update) multiple topic weights for a specific CourseCode
    /// Creates new weights if they don't exist, updates if they do. Total must be exactly 100%.
    /// </summary>
    [HttpPut("coursecode/{courseCodeId:guid}/bulk")]
    [Authorize(Roles = "Staff")]
    public async Task<ActionResult<BulkUpdateTopicWeightsResponse>> BulkUpdateTopicWeights(
        Guid courseCodeId,
        [FromBody] BulkUpdateTopicWeightsCommand command)
    {
        try
        {
            var userId = GetUserId();
            command.ConfiguredBy = userId;
            command.CourseCodeId = courseCodeId; // Enforce CourseCodeId from URL

            var result = await _mediator.Send(command);
            
            if (!result.Success && result.SuccessCount == 0)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get history of changes for a specific TopicWeight
    /// </summary>
    [HttpGet("{id:guid}/history")]
    [Authorize(Roles = "Staff,Lecturer")]
    public async Task<ActionResult<List<TopicWeightHistoryDto>>> GetHistory(Guid id)
    {
        var query = new GetTopicWeightHistoryQuery { TopicWeightId = id };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in claims");
        }
        return userId;
    }
}
