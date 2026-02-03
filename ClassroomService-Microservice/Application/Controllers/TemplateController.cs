using ClassroomService.Domain.Constants;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using HttpStatusCodes = Microsoft.AspNetCore.Http.StatusCodes;

namespace ClassroomService.Application.Controllers;

/// <summary>
/// Controller for template file management operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Templates")]
[Authorize]
public class TemplateController : ControllerBase
{
    private readonly ITemplateService _templateService;
    private readonly ILogger<TemplateController> _logger;

    public TemplateController(ITemplateService templateService, ILogger<TemplateController> logger)
    {
        _templateService = templateService;
        _logger = logger;
    }

    /// <summary>
    /// Gets template metadata or all templates with optional status filter
    /// </summary>
    /// <param name="isActive">Optional filter by active status (true/false). If not provided, returns active template metadata only.</param>
    /// <returns>Template metadata or list of templates based on filter</returns>
    /// <response code="200">Returns the template metadata or list of templates</response>
    /// <response code="500">Internal server error occurred</response>
    [HttpGet("metadata")]
    [ProducesResponseType(typeof(object), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<object>> GetTemplateMetadata([FromQuery] bool? isActive = null)
    {
        try
        {
            // If isActive filter is provided, return all templates (staff view)
            if (isActive.HasValue)
            {
                _logger.LogInformation("Request received for all templates with filter: IsActive={IsActive}", isActive);
                
                var templates = await _templateService.GetAllTemplatesAsync(isActive);
                
                _logger.LogInformation("Retrieved {Count} templates", templates.Count);
                return Ok(templates);
            }
            
            // Otherwise, return active template metadata (default view)
            _logger.LogInformation("Request received for active template metadata");
            
            var metadata = await _templateService.GetTemplateMetadataAsync();
            
            _logger.LogInformation("Template metadata returned successfully");
            return Ok(metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving template metadata");
            return Problem(
                title: "Error Retrieving Metadata",
                detail: "An error occurred while retrieving the template metadata",
                statusCode: HttpStatusCodes.Status500InternalServerError
            );
        }
    }

    /// <summary>
    /// Downloads a specific template file by ID
    /// </summary>
    /// <param name="id">Template ID to download (required)</param>
    /// <returns>Word document file stream</returns>
    /// <response code="200">Returns the Word document file</response>
    /// <response code="400">Template ID is required</response>
    /// <response code="404">Template not found</response>
    /// <response code="500">Internal server error occurred</response>
    [HttpGet("download")]
    [Produces(TemplateConstants.MimeTypes.WordDocument)]
    [ProducesResponseType(typeof(FileStreamResult), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), HttpStatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), HttpStatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DownloadTemplate([FromQuery] Guid id)
    {
        try
        {
            _logger.LogInformation("Request received to download template: {TemplateId}", id);
            
            var (fileStream, fileName, contentType) = await _templateService.GetTemplateForDownloadAsync(id);
            
            _logger.LogInformation("Template download successful, filename: {FileName}", fileName);
            
            return File(fileStream, contentType, fileName);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "Template file not found");
            return Problem(
                title: "Template Not Found",
                detail: ex.Message,
                statusCode: HttpStatusCodes.Status404NotFound
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading template");
            return Problem(
                title: "Error Downloading Template",
                detail: "An error occurred while downloading the template",
                statusCode: HttpStatusCodes.Status500InternalServerError
            );
        }
    }

    /// <summary>
    /// Uploads a new template file (staff only)
    /// </summary>
    /// <param name="uploadDto">Template upload data</param>
    /// <returns>Uploaded template information</returns>
    /// <response code="200">Template uploaded successfully</response>
    /// <response code="400">Invalid file or validation error</response>
    /// <response code="403">Forbidden - staff only</response>
    /// <response code="500">Internal server error occurred</response>
    [HttpPost("upload")]
    [Authorize(Roles = "Staff,Admin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(TemplateFileDto), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TemplateFileDto>> UploadTemplate([FromForm] TemplateUploadDto uploadDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";
            
            _logger.LogInformation("Template upload request received from user: {UserId}", userId);
            
            var templateDto = await _templateService.UploadTemplateAsync(
                uploadDto.File, 
                userId, 
                uploadDto.Description
            );
            
            _logger.LogInformation("Template uploaded successfully by user: {UserId}", userId);
            
            return Ok(templateDto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid template upload request");
            return Problem(
                title: "Invalid Upload",
                detail: ex.Message,
                statusCode: HttpStatusCodes.Status400BadRequest
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading template");
            return Problem(
                title: "Error Uploading Template",
                detail: "An error occurred while uploading the template",
                statusCode: HttpStatusCodes.Status500InternalServerError
            );
        }
    }

    /// <summary>
    /// Deletes a template file (staff only)
    /// </summary>
    /// <param name="id">Template ID to delete</param>
    /// <returns>Deletion result</returns>
    /// <response code="200">Template deleted successfully</response>
    /// <response code="400">Cannot delete active template</response>
    /// <response code="403">Forbidden - staff only</response>
    /// <response code="404">Template not found</response>
    /// <response code="500">Internal server error occurred</response>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Staff,Admin")]
    [ProducesResponseType(typeof(DeleteTemplateResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DeleteTemplateResponse), HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), HttpStatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DeleteTemplateResponse>> DeleteTemplate(Guid id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";
            
            _logger.LogInformation("Template delete request received for ID: {TemplateId} by user: {UserId}", id, userId);
            
            var deleted = await _templateService.DeleteTemplateAsync(id, userId);
            
            if (!deleted)
            {
                return NotFound(new DeleteTemplateResponse
                {
                    Success = false,
                    Message = $"Template with ID {id} was not found",
                    TemplateId = id
                });
            }
            
            _logger.LogInformation("Template deleted successfully: {TemplateId} by user: {UserId}", id, userId);
            
            return Ok(new DeleteTemplateResponse
            {
                Success = true,
                Message = "Template deleted successfully",
                TemplateId = id
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot delete active template: {TemplateId}", id);
            return BadRequest(new DeleteTemplateResponse
            {
                Success = false,
                Message = ex.Message,
                TemplateId = id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting template: {TemplateId}", id);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new DeleteTemplateResponse
            {
                Success = false,
                Message = "An error occurred while deleting the template"
            });
        }
    }

    /// <summary>
    /// Toggles the active status of a template (staff only)
    /// </summary>
    /// <param name="id">Template ID to toggle</param>
    /// <returns>Updated template information</returns>
    /// <response code="200">Template status toggled successfully</response>
    /// <response code="403">Forbidden - staff only</response>
    /// <response code="404">Template not found</response>
    /// <response code="500">Internal server error occurred</response>
    [HttpPatch("{id}/toggle-status")]
    [Authorize(Roles = "Staff,Admin")]
    [ProducesResponseType(typeof(TemplateFileDto), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), HttpStatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TemplateFileDto>> ToggleTemplateStatus(Guid id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";
            
            _logger.LogInformation("Toggle template status request for ID: {TemplateId} by user: {UserId}", id, userId);
            
            var template = await _templateService.ToggleTemplateStatusAsync(id, userId);
            
            _logger.LogInformation("Template status toggled successfully: {TemplateId}, IsActive: {IsActive}", id, template.IsActive);
            
            return Ok(template);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "Template not found: {TemplateId}", id);
            return Problem(
                title: "Template Not Found",
                detail: ex.Message,
                statusCode: HttpStatusCodes.Status404NotFound
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling template status: {TemplateId}", id);
            return Problem(
                title: "Error Toggling Status",
                detail: "An error occurred while toggling the template status",
                statusCode: HttpStatusCodes.Status500InternalServerError
            );
        }
    }
}
