using System.Text.Json.Serialization;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Application.Features.Courses.Queries;

/// <summary>
/// Data transfer object for Course
/// </summary>
public class CourseDto
{
    /// <summary>
    /// Course unique identifier
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Course code (from CourseCode entity)
    /// </summary>
    public string CourseCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Unique 6-character code to distinguish course sections
    /// </summary>
    public string UniqueCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Course code title (from CourseCode entity)
    /// </summary>
    public string CourseCodeTitle { get; set; } = string.Empty;
    
    /// <summary>
    /// Auto-generated course name (CourseCode + " - " + UniqueCode + " - " + Lecturer Name)
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Course description/details
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Academic term
    /// </summary>
    public string Term { get; set; } = string.Empty;
    
    /// <summary>
    /// Term start date
    /// </summary>
    public DateTime TermStartDate { get; set; }
    
    /// <summary>
    /// Term end date
    /// </summary>
    public DateTime TermEndDate { get; set; }
    
    /// <summary>
    /// The lecturer/instructor ID
    /// </summary>
    public Guid LecturerId { get; set; }
    
    /// <summary>
    /// The lecturer/instructor name
    /// </summary>
    public string LecturerName { get; set; } = string.Empty;
    
    /// <summary>
    /// The lecturer's profile picture URL
    /// </summary>
    public string? LecturerImage { get; set; }
    
    /// <summary>
    /// When the course was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Number of active enrollments
    /// </summary>
    public int EnrollmentCount { get; set; }
    
    /// <summary>
    /// Course approval status
    /// </summary>
    public CourseStatus Status { get; set; }
    
    /// <summary>
    /// Staff who approved/rejected
    /// </summary>
    public Guid? ApprovedBy { get; set; }
    
    /// <summary>
    /// Staff name who approved/rejected
    /// </summary>
    public string? ApprovedByName { get; set; }
    
    /// <summary>
    /// When approved
    /// </summary>
    public DateTime? ApprovedAt { get; set; }
    
    /// <summary>
    /// Approval comments
    /// </summary>
    public string? ApprovalComments { get; set; }
    
    /// <summary>
    /// Rejection reason
    /// </summary>
    public string? RejectionReason { get; set; }
    
    /// <summary>
    /// Can students enroll (Active status)
    /// </summary>
    public bool CanEnroll { get; set; }
    
    /// <summary>
    /// Whether this course requires an access code to join
    /// </summary>
    public bool RequiresAccessCode { get; set; }
    
    /// <summary>
    /// The access code (only shown to appropriate users)
    /// </summary>
    public string? AccessCode { get; set; }
    
    /// <summary>
    /// When the access code was created
    /// </summary>
    public DateTime? AccessCodeCreatedAt { get; set; }
    
    /// <summary>
    /// When the access code expires
    /// </summary>
    public DateTime? AccessCodeExpiresAt { get; set; }
    
    /// <summary>
    /// Whether the access code has expired
    /// </summary>
    public bool? IsAccessCodeExpired { get; set; }
    
    /// <summary>
    /// Optional course image URL or path
    /// </summary>
    public string? Img { get; set; }
    
    /// <summary>
    /// Course announcement/notice
    /// </summary>
    public string? Announcement { get; set; }
    
    /// <summary>
    /// Course syllabus file path/URL
    /// </summary>
    public string? SyllabusFile { get; set; }
    
    /// <summary>
    /// Department this course belongs to
    /// </summary>
    public string Department { get; set; } = string.Empty;
    
    /// <summary>
    /// Indicates if the requesting student is enrolled in the course (only populated for students)
    /// </summary>
    public bool? IsEnrolled { get; set; }
}