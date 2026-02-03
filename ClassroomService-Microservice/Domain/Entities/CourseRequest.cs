using ClassroomService.Domain.Common;
using ClassroomService.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ClassroomService.Domain.Entities;

public class CourseRequest : BaseAuditableEntity
{
    /// <summary>
    /// Reference to the CourseCode entity
    /// </summary>
    public Guid CourseCodeId { get; set; }
    
    /// <summary>
    /// Course description/details
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Reference to the Term entity
    /// </summary>
    [Required]
    public Guid TermId { get; set; }
    
    /// <summary>
    /// The lecturer requesting the course
    /// </summary>
    public Guid LecturerId { get; set; }
    
    /// <summary>
    /// Status of the course request
    /// </summary>
    public CourseRequestStatus Status { get; set; } = CourseRequestStatus.Pending;
    
    /// <summary>
    /// Reason provided by lecturer for the request
    /// </summary>
    public string? RequestReason { get; set; }
    
    /// <summary>
    /// Staff member who processed the request
    /// </summary>
    public Guid? ProcessedBy { get; set; }
    
    /// <summary>
    /// When the request was processed
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
    
    /// <summary>
    /// Comments from staff member
    /// </summary>
    public string? ProcessingComments { get; set; }
    
    /// <summary>
    /// The course created as a result of this request (if approved)
    /// </summary>
    public Guid? CreatedCourseId { get; set; }
    
    /// <summary>
    /// Initial course announcement (optional)
    /// </summary>
    [MaxLength(2000)]
    public string? Announcement { get; set; }
    
    /// <summary>
    /// Course syllabus file path (optional)
    /// </summary>
    [MaxLength(500)]
    public string? SyllabusFile { get; set; }

    // Navigation properties
    /// <summary>
    /// The course code/curriculum this request is for
    /// </summary>
    public virtual CourseCode CourseCode { get; set; } = null!;
    
    /// <summary>
    /// The term for this course request
    /// </summary>
    public virtual Term Term { get; set; } = null!;
    
    /// <summary>
    /// The created course (if request was approved)
    /// </summary>
    public virtual Course? CreatedCourse { get; set; }
}