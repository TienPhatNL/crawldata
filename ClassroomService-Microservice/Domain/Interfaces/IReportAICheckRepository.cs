using ClassroomService.Domain.Entities;

namespace ClassroomService.Domain.Interfaces;

/// <summary>
/// Repository interface for ReportAICheck operations
/// </summary>
public interface IReportAICheckRepository : IRepository<ReportAICheck>
{
    /// <summary>
    /// Get all AI checks for a specific report
    /// </summary>
    Task<IEnumerable<ReportAICheck>> GetChecksByReportIdAsync(Guid reportId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get the most recent AI check for a report
    /// </summary>
    Task<ReportAICheck?> GetLatestCheckAsync(Guid reportId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all AI checks for reports in a specific assignment
    /// </summary>
    Task<IEnumerable<ReportAICheck>> GetChecksByAssignmentAsync(Guid assignmentId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get AI checks performed by a specific lecturer
    /// </summary>
    Task<IEnumerable<ReportAICheck>> GetChecksByLecturerAsync(Guid lecturerId, CancellationToken cancellationToken = default);
}
