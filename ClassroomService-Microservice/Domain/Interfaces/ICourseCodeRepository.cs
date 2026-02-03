using ClassroomService.Domain.Entities;

namespace ClassroomService.Domain.Interfaces;

/// <summary>
/// Repository interface for CourseCode-specific operations
/// </summary>
public interface ICourseCodeRepository : IRepository<CourseCode>
{
    /// <summary>
    /// Get all active course codes
    /// </summary>
    Task<IEnumerable<CourseCode>> GetActiveCourseCodesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get course code by code string (e.g., "CS101")
    /// </summary>
    Task<CourseCode?> GetCourseCodeByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get course codes by department
    /// </summary>
    Task<IEnumerable<CourseCode>> GetCourseCodesByDepartmentAsync(string department, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a course code exists
    /// </summary>
    Task<bool> CourseCodeExistsAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get course code with all associated courses
    /// </summary>
    Task<CourseCode?> GetCourseCodeWithCoursesAsync(Guid courseCodeId, CancellationToken cancellationToken = default);
}
