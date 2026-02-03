using ClassroomService.Domain.Constants;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ClassroomService.Infrastructure.Services;

/// <summary>
/// Service for managing template file uploads and downloads
/// </summary>
public class TemplateService : ITemplateService
{
    private readonly ILogger<TemplateService> _logger;
    private readonly ITemplateFileRepository _templateFileRepository;
    private readonly string _uploadDirectory;

    public TemplateService(
        ILogger<TemplateService> logger, 
        ITemplateFileRepository templateFileRepository)
    {
        _logger = logger;
        _templateFileRepository = templateFileRepository;
        _uploadDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UploadedTemplates");
        
        // Ensure upload directory exists
        if (!Directory.Exists(_uploadDirectory))
        {
            Directory.CreateDirectory(_uploadDirectory);
            _logger.LogInformation("Created upload directory: {Directory}", _uploadDirectory);
        }
    }

    /// <summary>
    /// Uploads a new template file and sets it as active
    /// </summary>
    public async Task<TemplateFileDto> UploadTemplateAsync(IFormFile file, string uploadedBy, string? description)
    {
        try
        {
            _logger.LogInformation("Uploading template file: {FileName} by user: {UploadedBy}", file.FileName, uploadedBy);

            // Validate file
            ValidateFile(file);

            // Generate unique file name
            var fileExtension = Path.GetExtension(file.FileName);
            var storedFileName = $"template_{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(_uploadDirectory, storedFileName);

            // Save file to disk
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            _logger.LogInformation("File saved to disk: {FilePath}", filePath);

            // Create template entity
            var templateFile = new TemplateFile
            {
                Id = Guid.NewGuid(),
                OriginalFileName = file.FileName,
                StoredFileName = storedFileName,
                FilePath = filePath,
                FileSize = file.Length,
                ContentType = file.ContentType,
                UploadedBy = uploadedBy,
                UploadedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true,
                Description = description
            };

            // Save to database
            var savedTemplate = await _templateFileRepository.AddTemplateAsync(templateFile);

            _logger.LogInformation("Template uploaded successfully with ID: {TemplateId}", savedTemplate.Id);

            return MapToDto(savedTemplate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading template file");
            throw;
        }
    }

    /// <summary>
    /// Gets the currently active template file for download
    /// </summary>
    public async Task<(Stream FileStream, string FileName, string ContentType)> GetActiveTemplateAsync()
    {
        try
        {
            _logger.LogInformation("Retrieving active template");

            var template = await _templateFileRepository.GetActiveTemplateAsync();

            if (template == null)
            {
                _logger.LogWarning("No active template found");
                throw new FileNotFoundException("No active template is currently available");
            }

            if (!File.Exists(template.FilePath))
            {
                _logger.LogError("Template file not found on disk: {FilePath}", template.FilePath);
                throw new FileNotFoundException($"Template file not found: {template.OriginalFileName}");
            }

            var fileStream = new FileStream(template.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            _logger.LogInformation("Active template retrieved: {FileName}", template.OriginalFileName);

            return (fileStream, template.OriginalFileName, template.ContentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active template");
            throw;
        }
    }

    /// <summary>
    /// Gets a specific template file for download by ID
    /// </summary>
    public async Task<(Stream FileStream, string FileName, string ContentType)> GetTemplateForDownloadAsync(Guid templateId)
    {
        try
        {
            _logger.LogInformation("Retrieving template for download: {TemplateId}", templateId);

            var template = await _templateFileRepository.GetTemplateByIdAsync(templateId);

            if (template == null)
            {
                _logger.LogWarning("Template not found: {TemplateId}", templateId);
                throw new FileNotFoundException($"Template with ID {templateId} not found");
            }

            if (!File.Exists(template.FilePath))
            {
                _logger.LogError("Template file not found on disk: {FilePath}", template.FilePath);
                throw new FileNotFoundException($"Template file not found: {template.OriginalFileName}");
            }

            var fileStream = new FileStream(template.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            _logger.LogInformation("Template retrieved for download: {FileName}", template.OriginalFileName);

            return (fileStream, template.OriginalFileName, template.ContentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving template for download: {TemplateId}", templateId);
            throw;
        }
    }

    /// <summary>
    /// Deletes a template file
    /// </summary>
    public async Task<bool> DeleteTemplateAsync(Guid templateId, string deletedBy)
    {
        try
        {
            _logger.LogInformation("Deleting template: {TemplateId} by user: {DeletedBy}", templateId, deletedBy);

            var template = await _templateFileRepository.GetTemplateByIdAsync(templateId);

            if (template == null)
            {
                _logger.LogWarning("Template not found: {TemplateId}", templateId);
                return false;
            }

            // Prevent deletion of active templates
            if (template.IsActive)
            {
                _logger.LogWarning("Cannot delete active template: {TemplateId}", templateId);
                throw new InvalidOperationException("Cannot delete an active template. Please deactivate it first.");
            }

            // Delete physical file
            if (File.Exists(template.FilePath))
            {
                File.Delete(template.FilePath);
                _logger.LogInformation("Physical file deleted: {FilePath}", template.FilePath);
            }

            // Delete from database
            var deleted = await _templateFileRepository.DeleteTemplateAsync(templateId);

            if (deleted)
            {
                _logger.LogInformation("Template deleted successfully: {TemplateId}", templateId);
            }

            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting template: {TemplateId}", templateId);
            throw;
        }
    }

    /// <summary>
    /// Gets metadata about the current active template
    /// </summary>
    public async Task<TemplateMetadataDto> GetTemplateMetadataAsync()
    {
        try
        {
            _logger.LogInformation("Retrieving template metadata");

            var template = await _templateFileRepository.GetActiveTemplateAsync();

            if (template == null)
            {
                return new TemplateMetadataDto
                {
                    HasActiveTemplate = false,
                    Template = null,
                    Message = "No active template is currently available. Please contact staff to upload a template."
                };
            }

            return new TemplateMetadataDto
            {
                HasActiveTemplate = true,
                Template = MapToDto(template),
                Message = "Active template is available for download"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving template metadata");
            throw;
        }
    }

    /// <summary>
    /// Gets template information by ID
    /// </summary>
    public async Task<TemplateFileDto?> GetTemplateByIdAsync(Guid templateId)
    {
        try
        {
            _logger.LogInformation("Retrieving template by ID: {TemplateId}", templateId);

            var template = await _templateFileRepository.GetTemplateByIdAsync(templateId);

            return template != null ? MapToDto(template) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving template by ID: {TemplateId}", templateId);
            throw;
        }
    }

    /// <summary>
    /// Toggles the active status of a template
    /// </summary>
    public async Task<TemplateFileDto> ToggleTemplateStatusAsync(Guid templateId, string updatedBy)
    {
        try
        {
            _logger.LogInformation("Toggling template status: {TemplateId} by user: {UpdatedBy}", templateId, updatedBy);

            var template = await _templateFileRepository.GetTemplateByIdAsync(templateId);

            if (template == null)
            {
                throw new FileNotFoundException($"Template with ID {templateId} not found");
            }

            // Toggle the status
            template.IsActive = !template.IsActive;
            template.UpdatedAt = DateTime.UtcNow;

            var updatedTemplate = await _templateFileRepository.UpdateTemplateAsync(template);

            _logger.LogInformation("Template status toggled: {TemplateId}, IsActive: {IsActive}", templateId, updatedTemplate.IsActive);

            return MapToDto(updatedTemplate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling template status: {TemplateId}", templateId);
            throw;
        }
    }

    /// <summary>
    /// Gets all templates with optional status filter
    /// </summary>
    public async Task<List<TemplateFileDto>> GetAllTemplatesAsync(bool? isActive = null)
    {
        try
        {
            _logger.LogInformation("Retrieving all templates with filter: IsActive={IsActive}", isActive);

            var templates = await _templateFileRepository.GetAllTemplatesAsync(isActive);

            return templates.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all templates");
            throw;
        }
    }

    /// <summary>
    /// Validates the uploaded file
    /// </summary>
    private void ValidateFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is required and cannot be empty");
        }

        // Check file extension
        var allowedExtensions = new[] { ".docx" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(fileExtension))
        {
            throw new ArgumentException($"Invalid file type. Only {string.Join(", ", allowedExtensions)} files are allowed");
        }

        // Check MIME type
        if (file.ContentType != TemplateConstants.MimeTypes.WordDocument)
        {
            throw new ArgumentException($"Invalid content type. Expected {TemplateConstants.MimeTypes.WordDocument}");
        }

        // Check file size (max 5MB)
        var maxFileSize = TemplateConstants.MaxFileSizeBytes;
        if (file.Length > maxFileSize)
        {
            throw new ArgumentException($"File size exceeds the maximum allowed size of {maxFileSize / 1024 / 1024}MB");
        }
    }

    /// <summary>
    /// Maps TemplateFile entity to DTO
    /// </summary>
    private TemplateFileDto MapToDto(TemplateFile template)
    {
        return new TemplateFileDto
        {
            Id = template.Id,
            OriginalFileName = template.OriginalFileName,
            FileSize = template.FileSize,
            FileSizeFormatted = FormatFileSize(template.FileSize),
            UploadedBy = template.UploadedBy,
            UploadedAt = template.UploadedAt,
            UpdatedAt = template.UpdatedAt,
            IsActive = template.IsActive,
            Description = template.Description
        };
    }

    /// <summary>
    /// Formats file size in human-readable format
    /// </summary>
    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}
