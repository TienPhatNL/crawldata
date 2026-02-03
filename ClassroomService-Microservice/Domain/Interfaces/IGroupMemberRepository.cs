using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Domain.Interfaces;

/// <summary>
/// Repository interface for GroupMember-specific operations
/// </summary>
public interface IGroupMemberRepository : IRepository<GroupMember>
{
    /// <summary>
    /// Get all members of a specific group
    /// </summary>
    Task<IEnumerable<GroupMember>> GetMembersByGroupAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the leader of a specific group
    /// </summary>
    Task<GroupMember?> GetGroupLeaderAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a student is in a specific group
    /// </summary>
    Task<bool> IsStudentInGroupAsync(Guid groupId, Guid studentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get group member by student ID and group ID
    /// </summary>
    Task<GroupMember?> GetGroupMemberAsync(Guid groupId, Guid studentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all groups a student is part of
    /// </summary>
    Task<IEnumerable<GroupMember>> GetStudentGroupsAsync(Guid studentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get member count for a specific group
    /// </summary>
    Task<int> GetMemberCountAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get members by role
    /// </summary>
    Task<IEnumerable<GroupMember>> GetMembersByRoleAsync(Guid groupId, GroupMemberRole role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all group members for a specific course
    /// </summary>
    Task<IEnumerable<GroupMember>> GetMembersByCourseAsync(Guid courseId, CancellationToken cancellationToken = default);
}
