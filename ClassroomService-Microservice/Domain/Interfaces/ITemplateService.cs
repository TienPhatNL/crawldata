using ClassroomService.Domain.DTOs;
using Microsoft.AspNetCore.Http;

namespace ClassroomService.Domain.Interfaces;

/// <summary>
/// Service interface for template management operations
/// </summary>
public interface ITemplateService
{
    /// <summary>
    /// Uploads a new template file and sets it as active
    /// </summary>
    /// <param name="file">The template file to upload</param>
    /// <param name="uploadedBy">User ID of the staff member uploading</param>
    /// <param name="description">Optional description of the template</param>
    /// <returns>Template file DTO</returns>
    Task<TemplateFileDto> UploadTemplateAsync(IFormFile file, string uploadedBy, string? description);

    /// <summary>
    /// Gets the currently active template file for download
    /// </summary>
    /// <returns>File stream and metadata for download</returns>
    Task<(Stream FileStream, string FileName, string ContentType)> GetActiveTemplateAsync();

    /// <summary>
    /// Gets a specific template file for download by ID
    /// </summary>
    /// <param name="templateId">Template ID to download</param>
    /// <returns>File stream and metadata for download</returns>
    Task<(Stream FileStream, string FileName, string ContentType)> GetTemplateForDownloadAsync(Guid templateId);

    /// <summary>
    /// Deletes a template file
    /// </summary>
    /// <param name="templateId">Template ID to delete</param>
    /// <param name="deletedBy">User ID of the staff member deleting</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteTemplateAsync(Guid templateId, string deletedBy);

    /// <summary>
    /// Gets metadata about the current active template
    /// </summary>
    /// <returns>Template metadata DTO</returns>
    Task<TemplateMetadataDto> GetTemplateMetadataAsync();

    /// <summary>
    /// Gets template information by ID
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <returns>Template file DTO or null if not found</returns>
    Task<TemplateFileDto?> GetTemplateByIdAsync(Guid templateId);

    /// <summary>
    /// Toggles the active status of a template
    /// </summary>
    /// <param name="templateId">Template ID to toggle</param>
    /// <param name="updatedBy">User ID of the staff member toggling</param>
    /// <returns>Updated template file DTO</returns>
    Task<TemplateFileDto> ToggleTemplateStatusAsync(Guid templateId, string updatedBy);

    /// <summary>
    /// Gets all templates with optional status filter
    /// </summary>
    /// <param name="isActive">Optional filter by active status</param>
    /// <returns>List of template file DTOs</returns>
    Task<List<TemplateFileDto>> GetAllTemplatesAsync(bool? isActive = null);
}
