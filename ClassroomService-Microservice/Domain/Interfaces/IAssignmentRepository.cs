using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Domain.Interfaces;

/// <summary>
/// Repository interface for Assignment-specific operations
/// </summary>
public interface IAssignmentRepository : IRepository<Assignment>
{
    /// <summary>
    /// Get all assignments for a specific course
    /// </summary>
    Task<IEnumerable<Assignment>> GetAssignmentsByCourseAsync(Guid courseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get upcoming assignments for a course
    /// </summary>
    Task<IEnumerable<Assignment>> GetUpcomingAssignmentsAsync(Guid courseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get overdue assignments across all courses
    /// </summary>
    Task<IEnumerable<Assignment>> GetOverdueAssignmentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get assignments by status
    /// </summary>
    Task<IEnumerable<Assignment>> GetAssignmentsByStatusAsync(AssignmentStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get assignment with assigned groups loaded
    /// </summary>
    Task<Assignment?> GetAssignmentWithGroupsAsync(Guid assignmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active assignments for a course
    /// </summary>
    Task<IEnumerable<Assignment>> GetActiveAssignmentsAsync(Guid courseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get assignments that should transition to Open status
    /// </summary>
    Task<IEnumerable<Assignment>> GetAssignmentsToOpenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get assignments that should transition to Closed status
    /// </summary>
    Task<IEnumerable<Assignment>> GetAssignmentsToCloseAsync(CancellationToken cancellationToken = default);
}
