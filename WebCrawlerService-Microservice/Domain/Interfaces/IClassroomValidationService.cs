using WebCrawlerService.Domain.DTOs;

namespace WebCrawlerService.Domain.Interfaces;

/// <summary>
/// Service for validating assignment and group access with ClassroomService
/// </summary>
public interface IClassroomValidationService
{
    /// <summary>
    /// Validate if a user has access to an assignment
    /// </summary>
    Task<AssignmentValidationResponse> ValidateAssignmentAccessAsync(
        Guid assignmentId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate if a user is a member of a group
    /// </summary>
    Task<GroupValidationResponse> ValidateGroupMembershipAsync(
        Guid groupId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get detailed information about a group
    /// </summary>
    Task<GroupInfo?> GetGroupInfoAsync(
        Guid groupId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get detailed information about an assignment
    /// </summary>
    Task<AssignmentInfo?> GetAssignmentInfoAsync(
        Guid assignmentId,
        CancellationToken cancellationToken = default);
}
