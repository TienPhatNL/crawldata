using ClassroomService.Domain.Entities;

namespace ClassroomService.Domain.Interfaces;

/// <summary>
/// Repository interface for Group-specific operations
/// </summary>
public interface IGroupRepository : IRepository<Group>
{
    /// <summary>
    /// Get all groups for a specific course
    /// </summary>
    Task<IEnumerable<Group>> GetGroupsByCourseAsync(Guid courseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get groups assigned to a specific assignment
    /// </summary>
    Task<IEnumerable<Group>> GetGroupsByAssignmentAsync(Guid assignmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all groups a student is part of in a specific course
    /// </summary>
    Task<IEnumerable<Group>> GetStudentGroupsInCourseAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get group with all members loaded
    /// </summary>
    Task<Group?> GetGroupWithMembersAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get available groups (not locked and not full) for a course
    /// </summary>
    Task<IEnumerable<Group>> GetAvailableGroupsAsync(Guid courseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a group name exists in a course
    /// </summary>
    Task<bool> GroupNameExistsAsync(Guid courseId, string groupName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get group by name in a specific course
    /// </summary>
    Task<Group?> GetGroupByNameAsync(Guid courseId, string groupName, CancellationToken cancellationToken = default);
}
