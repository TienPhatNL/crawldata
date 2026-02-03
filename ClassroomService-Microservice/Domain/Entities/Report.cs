using ClassroomService.Domain.Common;
using ClassroomService.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassroomService.Domain.Entities;

/// <summary>
/// Represents a student/group submission for an assignment
/// </summary>
public class Report : BaseAuditableEntity
{
    /// <summary>
    /// The assignment this report is submitted for
    /// </summary>
    public Guid AssignmentId { get; set; }
    
    /// <summary>
    /// The group that submitted this report (null for individual submissions)
    /// </summary>
    public Guid? GroupId { get; set; }
    
    /// <summary>
    /// The user who submitted this report (student or group leader)
    /// </summary>
    public Guid SubmittedBy { get; set; }
    
    /// <summary>
    /// When the report was submitted
    /// </summary>
    public DateTime? SubmittedAt { get; set; }
    
    /// <summary>
    /// The actual submission content/text
    /// </summary>
    [MaxLength(50000)]
    public string Submission { get; set; } = string.Empty;
    
    /// <summary>
    /// Current status of the report
    /// </summary>
    public ReportStatus Status { get; set; } = ReportStatus.Draft;
    
    /// <summary>
    /// Grade awarded (null until graded)
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? Grade { get; set; }
    
    /// <summary>
    /// Instructor feedback on the submission
    /// </summary>
    [MaxLength(5000)]
    public string? Feedback { get; set; }
    
    /// <summary>
    /// The instructor who graded this report
    /// </summary>
    public Guid? GradedBy { get; set; }
    
    /// <summary>
    /// When the report was graded
    /// </summary>
    public DateTime? GradedAt { get; set; }
    
    /// <summary>
    /// Indicates if this is a group submission
    /// </summary>
    public bool IsGroupSubmission { get; set; } = false;
    
    /// <summary>
    /// Version number for tracking resubmissions
    /// </summary>
    public int Version { get; set; } = 1;
    
    /// <summary>
    /// URL of the uploaded file attachment (e.g., PDF, DOCX) stored in AWS S3
    /// Supports versioning - old URLs are preserved in ReportHistory
    /// </summary>
    [MaxLength(2048)]
    public string? FileUrl { get; set; }

    // Navigation properties
    /// <summary>
    /// The assignment this report belongs to
    /// </summary>
    public virtual Assignment Assignment { get; set; } = null!;
    
    /// <summary>
    /// The group that submitted this report (if group submission)
    /// </summary>
    public virtual Group? Group { get; set; }
    
    /// <summary>
    /// AI content detection checks performed on this report
    /// </summary>
    public virtual ICollection<ReportAICheck> AIChecks { get; set; } = new List<ReportAICheck>();
}
