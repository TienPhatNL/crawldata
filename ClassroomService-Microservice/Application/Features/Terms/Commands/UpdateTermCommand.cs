using MediatR;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ClassroomService.Application.Features.Terms.DTOs;

namespace ClassroomService.Application.Features.Terms.Commands;

/// <summary>
/// Command to update an existing term
/// </summary>
public class UpdateTermCommand : IRequest<UpdateTermResponse>
{
    /// <summary>
    /// Term ID to update
    /// </summary>
    [JsonIgnore]
    public Guid Id { get; set; }
    
    /// <summary>
    /// Updated term name (e.g., "Spring", "Fall", "Q1", "Semester 1")
    /// </summary>
    [StringLength(100, MinimumLength = 1)]
    public string? Name { get; set; }
    
    /// <summary>
    /// Updated description of the term
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Updated start date of the term
    /// </summary>
    public DateTime? StartDate { get; set; }
    
    /// <summary>
    /// Updated end date of the term (must be after StartDate if both are provided)
    /// </summary>
    public DateTime? EndDate { get; set; }
    
    /// <summary>
    /// Updated active status
    /// </summary>
    public bool? IsActive { get; set; }
    
    /// <summary>
    /// Staff member performing the update
    /// </summary>
    [JsonIgnore]
    public Guid UpdatedBy { get; set; }
}

/// <summary>
/// Response for update term command
/// </summary>
public class UpdateTermResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public TermDto? Term { get; set; }
}
