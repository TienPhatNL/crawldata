using ClassroomService.Domain.Common;
using ClassroomService.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ClassroomService.Domain.Entities;

public class Course : BaseAuditableEntity
{
    /// <summary>
    /// Reference to the CourseCode entity
    /// </summary>
    public Guid CourseCodeId { get; set; }
    
    /// <summary>
    /// Unique alphanumeric code to distinguish multiple sections (e.g., "01", "02", or custom codes)
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string UniqueCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Auto-generated course name: CourseCode + " - " + UniqueCode + " - " + Lecturer Full Name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Course description/details (previously called Name)
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// The lecturer/instructor for this course section
    /// </summary>
    public Guid LecturerId { get; set; }
    
    /// <summary>
    /// Reference to the Term entity
    /// </summary>
    [Required]
    public Guid TermId { get; set; }
    
    /// <summary>
    /// Course approval status
    /// </summary>
    public CourseStatus Status { get; set; } = CourseStatus.PendingApproval;
    
    /// <summary>
    /// Staff member who approved/rejected
    /// </summary>
    public Guid? ApprovedBy { get; set; }
    
    /// <summary>
    /// When course was approved
    /// </summary>
    public DateTime? ApprovedAt { get; set; }
    
    /// <summary>
    /// Staff approval comments
    /// </summary>
    public string? ApprovalComments { get; set; }
    
    /// <summary>
    /// Rejection reason if rejected
    /// </summary>
    public string? RejectionReason { get; set; }
    
    /// <summary>
    /// Optional course image URL or path
    /// </summary>
    public string? Img { get; set; }
    
    /// <summary>
    /// Course announcement/notice text
    /// </summary>
    [MaxLength(2000)]
    public string? Announcement { get; set; }
    
    /// <summary>
    /// Course syllabus/material file path (PDF, Word, etc.)
    /// </summary>
    [MaxLength(500)]
    public string? SyllabusFile { get; set; }
    
    // Access Code Properties
    public string? AccessCode { get; set; }
    public bool RequiresAccessCode { get; set; } = false;
    public DateTime? AccessCodeCreatedAt { get; set; }
    public DateTime? AccessCodeExpiresAt { get; set; }
    public int AccessCodeAttempts { get; set; } = 0;
    public DateTime? LastAccessCodeAttempt { get; set; }

    // Navigation properties
    /// <summary>
    /// The course code/curriculum this course section belongs to
    /// </summary>
    public virtual CourseCode CourseCode { get; set; } = null!;
    
    /// <summary>
    /// The term for this course
    /// </summary>
    public virtual Term Term { get; set; } = null!;
    
    /// <summary>
    /// Students enrolled in this course section
    /// </summary>
    public virtual ICollection<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();
    
    /// <summary>
    /// Assignments for this course
    /// </summary>
    public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    
    /// <summary>
    /// Groups in this course
    /// </summary>
    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();
    
    /// <summary>
    /// Chats in this course
    /// </summary>
    public virtual ICollection<Chat> Chats { get; set; } = new List<Chat>();
    
    /// <summary>
    /// Custom topic weight overrides for this specific course
    /// </summary>
    public virtual ICollection<TopicWeight> CustomTopicWeights { get; set; } = new List<TopicWeight>();
}