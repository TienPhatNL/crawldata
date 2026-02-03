using ClassroomService.Domain.Common;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Domain.Interfaces;

public interface ISupportRequestRepository : IRepository<SupportRequest>
{
    /// <summary>
    /// Get pending support requests with pagination (for staff)
    /// </summary>
    Task<PagedResult<SupportRequest>> GetPendingSupportRequestsAsync(
        Guid? courseId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get support requests for a specific user
    /// </summary>
    Task<PagedResult<SupportRequest>> GetMySupportRequestsAsync(
        Guid userId,
        Guid? courseId,
        SupportRequestStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get support requests assigned to a specific staff member
    /// </summary>
    Task<PagedResult<SupportRequest>> GetStaffSupportRequestsAsync(
        Guid staffId,
        SupportRequestStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific support request with course details
    /// </summary>
    Task<SupportRequest?> GetSupportRequestByIdAsync(
        Guid requestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if user has an active (Pending or Accepted) support request in a course
    /// </summary>
    Task<SupportRequest?> GetActiveRequestForUserInCourseAsync(
        Guid userId,
        Guid courseId,
        CancellationToken cancellationToken = default);
}
