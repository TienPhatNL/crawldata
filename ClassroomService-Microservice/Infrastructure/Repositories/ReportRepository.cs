using Microsoft.EntityFrameworkCore;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Infrastructure.Persistence;

namespace ClassroomService.Infrastructure.Repositories;

public class ReportRepository : Repository<Report>, IReportRepository
{
    public ReportRepository(ClassroomDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Report>> GetReportsByAssignmentAsync(Guid assignmentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.AssignmentId == assignmentId)
            .Include(r => r.Assignment)
                .ThenInclude(a => a.Course)
            .Include(r => r.Group)
                .ThenInclude(g => g.Members)
            .OrderByDescending(r => r.SubmittedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Report>> GetReportsByGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.GroupId == groupId)
            .Include(r => r.Assignment)
            .Include(r => r.Group)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Report>> GetReportsByStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.SubmittedBy == studentId)
            .Include(r => r.Assignment)
                .ThenInclude(a => a.Course)
            .Include(r => r.Group)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Report?> GetReportWithDetailsAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.Assignment)
                .ThenInclude(a => a.Course)
                    .ThenInclude(c => c.CourseCode)
            .Include(r => r.Assignment)
                .ThenInclude(a => a.Topic)
            .Include(r => r.Group)
                .ThenInclude(g => g.Members)
            .FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);
    }

    public async Task<IEnumerable<Report>> GetSubmissionsForGradingAsync(Guid assignmentId, ReportStatus[] statuses, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.AssignmentId == assignmentId && statuses.Contains(r.Status))
            .Include(r => r.Assignment)
            .Include(r => r.Group)
                .ThenInclude(g => g.Members)
            .OrderBy(r => r.SubmittedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Report>> GetLateSubmissionsAsync(Guid assignmentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.AssignmentId == assignmentId && r.Status == ReportStatus.Late)
            .Include(r => r.Assignment)
            .Include(r => r.Group)
            .OrderByDescending(r => r.SubmittedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Report?> GetGroupSubmissionAsync(Guid assignmentId, Guid groupId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.Assignment)
            .Include(r => r.Group)
                .ThenInclude(g => g.Members)
            .FirstOrDefaultAsync(r => r.AssignmentId == assignmentId && r.GroupId == groupId, cancellationToken);
    }

    public async Task<Report?> GetStudentSubmissionAsync(Guid assignmentId, Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.Assignment)
            .Include(r => r.Group)
            .FirstOrDefaultAsync(r => r.AssignmentId == assignmentId && r.SubmittedBy == studentId && !r.IsGroupSubmission, cancellationToken);
    }

    public async Task<bool> HasSubmittedAsync(Guid assignmentId, Guid? groupId, Guid? studentId, CancellationToken cancellationToken = default)
    {
        if (groupId.HasValue)
        {
            return await _dbSet
                .AnyAsync(r => r.AssignmentId == assignmentId && r.GroupId == groupId, cancellationToken);
        }
        else if (studentId.HasValue)
        {
            return await _dbSet
                .AnyAsync(r => r.AssignmentId == assignmentId && r.SubmittedBy == studentId && !r.IsGroupSubmission, cancellationToken);
        }
        
        return false;
    }

    public async Task<IEnumerable<Report>> GetReportsByStatusAsync(ReportStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.Status == status)
            .Include(r => r.Assignment)
                .ThenInclude(a => a.Course)
            .Include(r => r.Group)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<(int TotalSubmissions, int Graded, int Pending, decimal? AverageGrade)> GetReportStatisticsAsync(Guid assignmentId, CancellationToken cancellationToken = default)
    {
        var reports = await _dbSet
            .Where(r => r.AssignmentId == assignmentId && r.Status != ReportStatus.Draft)
            .ToListAsync(cancellationToken);

        var totalSubmissions = reports.Count;
        var graded = reports.Count(r => r.Status == ReportStatus.Graded);
        var pending = reports.Count(r => r.Status == ReportStatus.Submitted || r.Status == ReportStatus.Resubmitted || r.Status == ReportStatus.UnderReview);
        var averageGrade = reports.Where(r => r.Grade.HasValue).Any() 
            ? reports.Where(r => r.Grade.HasValue).Average(r => r.Grade!.Value) 
            : (decimal?)null;

        return (totalSubmissions, graded, pending, averageGrade);
    }

    public async Task<IEnumerable<Report>> GetReportsForExportAsync(Guid assignmentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.AssignmentId == assignmentId && r.Status != ReportStatus.Draft)
            .Include(r => r.Assignment)
                .ThenInclude(a => a.Course)
                    .ThenInclude(c => c.CourseCode)
            .Include(r => r.Group)
                .ThenInclude(g => g.Members)
            .OrderBy(r => r.IsGroupSubmission)
                .ThenBy(r => r.SubmittedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Report>> GetReportsByCourseAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.Assignment)
                .ThenInclude(a => a.Course)
            .Include(r => r.Group)
            .Where(r => r.Assignment.CourseId == courseId)
            .OrderByDescending(r => r.SubmittedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Report>> GetReportsByIdsAsync(IEnumerable<Guid> reportIds, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => reportIds.Contains(r.Id))
            .Include(r => r.Assignment)
            .Include(r => r.Group)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Report>> GetReportsRequiringGradingAsync(Guid? courseId = null, Guid? assignmentId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(r => r.Assignment)
                .ThenInclude(a => a.Course)
            .Include(r => r.Group!)
                .ThenInclude(g => g.Members)
            .Where(r => r.Status == ReportStatus.Submitted || 
                       r.Status == ReportStatus.Resubmitted || 
                       r.Status == ReportStatus.UnderReview);

        if (courseId.HasValue)
        {
            query = query.Where(r => r.Assignment.CourseId == courseId.Value);
        }

        if (assignmentId.HasValue)
        {
            query = query.Where(r => r.AssignmentId == assignmentId.Value);
        }

        return await query
            .OrderBy(r => r.SubmittedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Report>> GetLateSubmissionsByCourseAsync(Guid? courseId = null, Guid? assignmentId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(r => r.Assignment)
                .ThenInclude(a => a.Course)
            .Include(r => r.Group!)
                .ThenInclude(g => g.Members)
            .Where(r => r.Status == ReportStatus.Late);

        if (courseId.HasValue)
        {
            query = query.Where(r => r.Assignment.CourseId == courseId.Value);
        }

        if (assignmentId.HasValue)
        {
            query = query.Where(r => r.AssignmentId == assignmentId.Value);
        }

        return await query
            .OrderByDescending(r => r.SubmittedAt)
            .ToListAsync(cancellationToken);
    }
}
