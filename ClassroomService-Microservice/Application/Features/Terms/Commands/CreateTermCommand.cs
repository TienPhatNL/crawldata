using MediatR;
using System.ComponentModel.DataAnnotations;
using ClassroomService.Application.Features.Terms.DTOs;

namespace ClassroomService.Application.Features.Terms.Commands;

/// <summary>
/// Command to create a new term
/// </summary>
public class CreateTermCommand : IRequest<CreateTermResponse>
{
    /// <summary>
    /// Term name (e.g., "Spring", "Fall", "Q1", "Semester 1")
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional description of the term
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Start date of the term
    /// </summary>
    [Required]
    public DateTime StartDate { get; set; }
    
    /// <summary>
    /// End date of the term (must be after StartDate)
    /// </summary>
    [Required]
    public DateTime EndDate { get; set; }
    
    /// <summary>
    /// Whether this term is active
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Response for create term command
/// </summary>
public class CreateTermResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public TermDto? Term { get; set; }
}
