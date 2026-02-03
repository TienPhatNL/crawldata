using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Application.Features.Dashboard.Helpers;

/// <summary>
/// Helper class to retrieve student reports including both individual and group submissions
/// </summary>
public static class StudentReportHelper
{
    /// <summary>
    /// Gets all reports for a student across specified assignments, including group submissions
    /// </summary>
    /// <param name="unitOfWork">Unit of work instance</param>
    /// <param name="studentId">Student ID</param>
    /// <param name="assignmentIds">List of assignment IDs to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of reports that belong to the student (individual or through group membership)</returns>
    public static async Task<List<Report>> GetStudentReportsAsync(
        IUnitOfWork unitOfWork,
        Guid studentId,
        IEnumerable<Guid> assignmentIds,
        CancellationToken cancellationToken)
    {
        var assignmentIdList = assignmentIds.ToList();
        
        if (!assignmentIdList.Any())
            return new List<Report>();
        
        // Get all reports for these assignments
        var allReports = await unitOfWork.Reports
            .GetManyAsync(r => assignmentIdList.Contains(r.AssignmentId), cancellationToken);
        
        var studentReports = new List<Report>();
        
        foreach (var report in allReports)
        {
            // Individual submission - check if student submitted
            if (!report.IsGroupSubmission && report.SubmittedBy == studentId)
            {
                studentReports.Add(report);
            }
            // Group submission - check if student is in the group
            else if (report.IsGroupSubmission && report.GroupId.HasValue)
            {
                var groupMember = await unitOfWork.GroupMembers
                    .GetAsync(gm => gm.GroupId == report.GroupId.Value && 
                                   gm.Enrollment.StudentId == studentId,
                             cancellationToken);
                
                if (groupMember != null)
                {
                    studentReports.Add(report);
                }
            }
        }
        
        return studentReports;
    }
    
    /// <summary>
    /// Gets a single report for a student for a specific assignment (individual or group)
    /// </summary>
    /// <param name="unitOfWork">Unit of work instance</param>
    /// <param name="studentId">Student ID</param>
    /// <param name="assignmentId">Assignment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The report if found, null otherwise</returns>
    public static async Task<Report?> GetStudentReportForAssignmentAsync(
        IUnitOfWork unitOfWork,
        Guid studentId,
        Guid assignmentId,
        CancellationToken cancellationToken)
    {
        var reports = await GetStudentReportsAsync(
            unitOfWork, 
            studentId, 
            new[] { assignmentId }, 
            cancellationToken);
        
        return reports.FirstOrDefault();
    }
}
