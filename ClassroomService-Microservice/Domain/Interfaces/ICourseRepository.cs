using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Domain.Interfaces;

/// <summary>
/// Repository interface for Course-specific operations
/// </summary>
public interface ICourseRepository : IRepository<Course>
{
    /// <summary>
    /// Get courses taught by a specific lecturer
    /// </summary>
    Task<IEnumerable<Course>> GetCoursesByLecturerAsync(Guid lecturerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get courses for a specific term
    /// </summary>
    Task<IEnumerable<Course>> GetCoursesByTermAsync(Guid termId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get course by access code
    /// </summary>
    Task<Course?> GetCourseByAccessCodeAsync(string accessCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get course by unique code
    /// </summary>
    Task<Course?> GetCourseByUniqueCodeAsync(string uniqueCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get course with all enrollments loaded
    /// </summary>
    Task<Course?> GetCourseWithEnrollmentsAsync(Guid courseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get courses by status (Active, Inactive, etc.)
    /// </summary>
    Task<IEnumerable<Course>> GetCoursesByStatusAsync(CourseStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active courses for a specific term
    /// </summary>
    Task<IEnumerable<Course>> GetActiveCoursesForTermAsync(Guid termId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get course with related data (enrollments, assignments, groups)
    /// </summary>
    Task<Course?> GetCourseWithDetailsAsync(Guid courseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get courses by lecturer ID
    /// </summary>
    Task<IEnumerable<Course>> GetCoursesByLecturerIdAsync(Guid lecturerId, CancellationToken cancellationToken = default);
}
