using ClassroomService.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassroomService.Domain.Entities;

/// <summary>
/// Represents an AI content detection check performed on a report
/// </summary>
public class ReportAICheck : BaseAuditableEntity
{
    /// <summary>
    /// The report that was checked
    /// </summary>
    public Guid ReportId { get; set; }
    
    /// <summary>
    /// AI detection percentage (0.00 - 100.00)
    /// Higher percentage indicates more likely AI-generated
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal AIPercentage { get; set; }
    
    /// <summary>
    /// AI detection service provider used
    /// </summary>
    [MaxLength(100)]
    public string Provider { get; set; } = string.Empty;
    
    /// <summary>
    /// Raw JSON response from AI detection service
    /// </summary>
    [MaxLength(5000)]
    public string? RawResponse { get; set; }
    
    /// <summary>
    /// The lecturer who initiated the AI check
    /// </summary>
    public Guid CheckedBy { get; set; }
    
    /// <summary>
    /// When the AI check was performed
    /// </summary>
    public DateTime CheckedAt { get; set; }
    
    /// <summary>
    /// Optional notes from the lecturer about this check
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    // Navigation properties
    /// <summary>
    /// The report that was checked
    /// </summary>
    public virtual Report Report { get; set; } = null!;
}
