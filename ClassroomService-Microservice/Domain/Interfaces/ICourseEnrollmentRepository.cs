using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Domain.Interfaces;

/// <summary>
/// Repository interface for CourseEnrollment-specific operations
/// </summary>
public interface ICourseEnrollmentRepository : IRepository<CourseEnrollment>
{
    /// <summary>
    /// Get all enrollments for a specific course
    /// </summary>
    Task<IEnumerable<CourseEnrollment>> GetEnrollmentsByCourseAsync(Guid courseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all enrollments for a specific student
    /// </summary>
    Task<IEnumerable<CourseEnrollment>> GetEnrollmentsByStudentAsync(Guid studentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active enrollments for a specific student
    /// </summary>
    Task<IEnumerable<CourseEnrollment>> GetActiveEnrollmentsAsync(Guid studentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a student is enrolled in a specific course
    /// </summary>
    Task<bool> IsStudentEnrolledAsync(Guid courseId, Guid studentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get enrollment for a specific student in a specific course
    /// </summary>
    Task<CourseEnrollment?> GetEnrollmentAsync(Guid courseId, Guid studentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get enrollments by status
    /// </summary>
    Task<IEnumerable<CourseEnrollment>> GetEnrollmentsByStatusAsync(EnrollmentStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get count of active enrollments for a course
    /// </summary>
    Task<int> GetActiveEnrollmentCountAsync(Guid courseId, CancellationToken cancellationToken = default);
}
