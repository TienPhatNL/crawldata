using ClassroomService.Domain.Common;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Domain.Entities;

public class CourseEnrollment : BaseAuditableEntity
{
    public Guid CourseId { get; set; }
    public Guid StudentId { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? UnenrolledAt { get; set; }
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
    public string? UnenrollmentReason { get; set; }
    public Guid? UnenrolledBy { get; set; } // Who unenrolled (admin, lecturer, or self)

    // Navigation properties (no User entity since it's in UserService)
    public virtual Course Course { get; set; } = null!;
}