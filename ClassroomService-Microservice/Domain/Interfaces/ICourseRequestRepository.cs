using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Domain.Interfaces;

/// <summary>
/// Repository interface for CourseRequest-specific operations
/// </summary>
public interface ICourseRequestRepository : IRepository<CourseRequest>
{
    /// <summary>
    /// Get all pending course requests
    /// </summary>
    Task<IEnumerable<CourseRequest>> GetPendingRequestsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get course requests by lecturer
    /// </summary>
    Task<IEnumerable<CourseRequest>> GetRequestsByLecturerAsync(Guid lecturerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get course requests by status
    /// </summary>
    Task<IEnumerable<CourseRequest>> GetRequestsByStatusAsync(CourseRequestStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get course request with related entities (CourseCode, Term, CreatedCourse)
    /// </summary>
    Task<CourseRequest?> GetRequestWithDetailsAsync(Guid requestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get requests by term
    /// </summary>
    Task<IEnumerable<CourseRequest>> GetRequestsByTermAsync(Guid termId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a similar request exists (same lecturer, course code, term)
    /// </summary>
    Task<bool> SimilarRequestExistsAsync(Guid lecturerId, Guid courseCodeId, Guid termId, CancellationToken cancellationToken = default);
}
