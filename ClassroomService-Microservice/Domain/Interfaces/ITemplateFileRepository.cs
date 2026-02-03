using ClassroomService.Domain.Entities;

namespace ClassroomService.Domain.Interfaces;

/// <summary>
/// Repository interface for template file operations
/// </summary>
public interface ITemplateFileRepository
{
    /// <summary>
    /// Gets the currently active template
    /// </summary>
    /// <returns>Active template file or null if none exists</returns>
    Task<TemplateFile?> GetActiveTemplateAsync();

    /// <summary>
    /// Gets a template by its ID
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <returns>Template file or null if not found</returns>
    Task<TemplateFile?> GetTemplateByIdAsync(Guid id);

    /// <summary>
    /// Adds a new template to the database
    /// </summary>
    /// <param name="template">Template entity to add</param>
    /// <returns>Added template entity</returns>
    Task<TemplateFile> AddTemplateAsync(TemplateFile template);

    /// <summary>
    /// Deactivates all templates (called before activating a new one)
    /// </summary>
    /// <returns>Number of templates deactivated</returns>
    Task<int> DeactivateAllTemplatesAsync();

    /// <summary>
    /// Deletes a template from the database
    /// </summary>
    /// <param name="id">Template ID to delete</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteTemplateAsync(Guid id);

    /// <summary>
    /// Checks if a template exists by ID
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> ExistsAsync(Guid id);

    /// <summary>
    /// Gets all templates with optional status filter
    /// </summary>
    /// <param name="isActive">Optional filter by active status</param>
    /// <returns>List of templates</returns>
    Task<List<TemplateFile>> GetAllTemplatesAsync(bool? isActive = null);

    /// <summary>
    /// Updates a template
    /// </summary>
    /// <param name="template">Template to update</param>
    /// <returns>Updated template</returns>
    Task<TemplateFile> UpdateTemplateAsync(TemplateFile template);
}
