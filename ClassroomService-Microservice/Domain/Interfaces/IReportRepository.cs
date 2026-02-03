using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Domain.Interfaces;

/// <summary>
/// Repository interface for Report-specific operations
/// </summary>
public interface IReportRepository : IRepository<Report>
{
    /// <summary>
    /// Get all reports for a specific assignment
    /// </summary>
    Task<IEnumerable<Report>> GetReportsByAssignmentAsync(Guid assignmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get reports for a specific group
    /// </summary>
    Task<IEnumerable<Report>> GetReportsByGroupAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all reports submitted by a specific student
    /// </summary>
    Task<IEnumerable<Report>> GetReportsByStudentAsync(Guid studentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a report with full details (includes Assignment and Group)
    /// </summary>
    Task<Report?> GetReportWithDetailsAsync(Guid reportId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get submissions needing grading for an assignment
    /// </summary>
    Task<IEnumerable<Report>> GetSubmissionsForGradingAsync(Guid assignmentId, ReportStatus[] statuses, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get late submissions for an assignment
    /// </summary>
    Task<IEnumerable<Report>> GetLateSubmissionsAsync(Guid assignmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific group's submission for an assignment
    /// </summary>
    Task<Report?> GetGroupSubmissionAsync(Guid assignmentId, Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific student's submission for an assignment
    /// </summary>
    Task<Report?> GetStudentSubmissionAsync(Guid assignmentId, Guid studentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a user or group has already submitted for an assignment
    /// </summary>
    Task<bool> HasSubmittedAsync(Guid assignmentId, Guid? groupId, Guid? studentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get reports by status
    /// </summary>
    Task<IEnumerable<Report>> GetReportsByStatusAsync(ReportStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get report statistics for an assignment
    /// </summary>
    Task<(int TotalSubmissions, int Graded, int Pending, decimal? AverageGrade)> GetReportStatisticsAsync(Guid assignmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all reports for export (with assignment details)
    /// </summary>
    Task<IEnumerable<Report>> GetReportsForExportAsync(Guid assignmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all reports for a specific course
    /// </summary>
    Task<IEnumerable<Report>> GetReportsByCourseAsync(Guid courseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get reports by multiple IDs (for bulk operations)
    /// </summary>
    Task<IEnumerable<Report>> GetReportsByIdsAsync(IEnumerable<Guid> reportIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get reports requiring grading (Submitted, Resubmitted, UnderReview)
    /// </summary>
    Task<IEnumerable<Report>> GetReportsRequiringGradingAsync(Guid? courseId = null, Guid? assignmentId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get late submissions by course or assignment
    /// </summary>
    Task<IEnumerable<Report>> GetLateSubmissionsByCourseAsync(Guid? courseId = null, Guid? assignmentId = null, CancellationToken cancellationToken = default);
}
