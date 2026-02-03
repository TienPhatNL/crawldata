using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ClassroomService.Domain.DTOs;

/// <summary>
/// DTO for uploading a template file
/// </summary>
public class TemplateUploadDto
{
    /// <summary>
    /// The template file to upload (must be .docx)
    /// </summary>
    [Required(ErrorMessage = "File is required")]
    public IFormFile File { get; set; } = null!;

    /// <summary>
    /// Optional description of the template
    /// </summary>
    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }
}
