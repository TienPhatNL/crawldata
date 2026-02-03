using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClassroomService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for ReportAICheck operations
/// </summary>
public class ReportAICheckRepository : Repository<ReportAICheck>, IReportAICheckRepository
{
    public ReportAICheckRepository(ClassroomDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ReportAICheck>> GetChecksByReportIdAsync(
        Guid reportId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.ReportAIChecks
            .Where(c => c.ReportId == reportId)
            .OrderByDescending(c => c.CheckedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ReportAICheck?> GetLatestCheckAsync(
        Guid reportId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.ReportAIChecks
            .Where(c => c.ReportId == reportId)
            .OrderByDescending(c => c.CheckedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<ReportAICheck>> GetChecksByAssignmentAsync(
        Guid assignmentId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.ReportAIChecks
            .Include(c => c.Report)
            .Where(c => c.Report.AssignmentId == assignmentId)
            .OrderByDescending(c => c.CheckedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ReportAICheck>> GetChecksByLecturerAsync(
        Guid lecturerId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.ReportAIChecks
            .Where(c => c.CheckedBy == lecturerId)
            .OrderByDescending(c => c.CheckedAt)
            .ToListAsync(cancellationToken);
    }
}
