using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClassroomService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for template file operations
/// </summary>
public class TemplateFileRepository : ITemplateFileRepository
{
    private readonly ClassroomDbContext _context;
    private readonly ILogger<TemplateFileRepository> _logger;

    public TemplateFileRepository(ClassroomDbContext context, ILogger<TemplateFileRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets the currently active template
    /// </summary>
    public async Task<TemplateFile?> GetActiveTemplateAsync()
    {
        try
        {
            return await _context.TemplateFiles
                .Where(t => t.IsActive)
                .OrderByDescending(t => t.UploadedAt)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active template");
            throw;
        }
    }

    /// <summary>
    /// Gets a template by its ID
    /// </summary>
    public async Task<TemplateFile?> GetTemplateByIdAsync(Guid id)
    {
        try
        {
            return await _context.TemplateFiles
                .FirstOrDefaultAsync(t => t.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving template by ID: {TemplateId}", id);
            throw;
        }
    }

    /// <summary>
    /// Adds a new template to the database
    /// </summary>
    public async Task<TemplateFile> AddTemplateAsync(TemplateFile template)
    {
        try
        {
            await _context.TemplateFiles.AddAsync(template);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Template added to database: {TemplateId}", template.Id);
            
            return template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding template to database");
            throw;
        }
    }

    /// <summary>
    /// Deactivates all templates (called before activating a new one)
    /// </summary>
    public async Task<int> DeactivateAllTemplatesAsync()
    {
        try
        {
            var activeTemplates = await _context.TemplateFiles
                .Where(t => t.IsActive)
                .ToListAsync();

            foreach (var template in activeTemplates)
            {
                template.IsActive = false;
                template.UpdatedAt = DateTime.UtcNow;
            }

            var count = await _context.SaveChangesAsync();
            
            _logger.LogInformation("Deactivated {Count} templates", activeTemplates.Count);
            
            return activeTemplates.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating templates");
            throw;
        }
    }

    /// <summary>
    /// Deletes a template from the database
    /// </summary>
    public async Task<bool> DeleteTemplateAsync(Guid id)
    {
        try
        {
            var template = await GetTemplateByIdAsync(id);
            
            if (template == null)
            {
                return false;
            }

            _context.TemplateFiles.Remove(template);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Template deleted from database: {TemplateId}", id);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting template from database: {TemplateId}", id);
            throw;
        }
    }

    /// <summary>
    /// Checks if a template exists by ID
    /// </summary>
    public async Task<bool> ExistsAsync(Guid id)
    {
        try
        {
            return await _context.TemplateFiles
                .AnyAsync(t => t.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if template exists: {TemplateId}", id);
            throw;
        }
    }

    /// <summary>
    /// Gets all templates with optional status filter
    /// </summary>
    public async Task<List<TemplateFile>> GetAllTemplatesAsync(bool? isActive = null)
    {
        try
        {
            var query = _context.TemplateFiles.AsQueryable();

            if (isActive.HasValue)
            {
                query = query.Where(t => t.IsActive == isActive.Value);
            }

            return await query
                .OrderByDescending(t => t.UploadedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all templates");
            throw;
        }
    }

    /// <summary>
    /// Updates a template
    /// </summary>
    public async Task<TemplateFile> UpdateTemplateAsync(TemplateFile template)
    {
        try
        {
            _context.TemplateFiles.Update(template);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Template updated: {TemplateId}", template.Id);
            
            return template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating template: {TemplateId}", template.Id);
            throw;
        }
    }
}
