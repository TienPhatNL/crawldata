using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClassroomService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for ReportHistory entity
/// </summary>
public class ReportHistoryRepository : Repository<ReportHistory>, IReportHistoryRepository
{
    public ReportHistoryRepository(ClassroomDbContext context) : base(context)
    {
    }
    
    /// <inheritdoc/>
    public async Task<List<ReportHistory>> GetReportHistoryAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        return await _context.ReportHistories
            .Where(h => h.ReportId == reportId)
            .OrderByDescending(h => h.ChangedAt)
            .ThenByDescending(h => h.Version)
            .ToListAsync(cancellationToken);
    }
    
    /// <inheritdoc/>
    public async Task<ReportHistory?> GetVersionAsync(Guid reportId, int version, CancellationToken cancellationToken = default)
    {
        // Get all records for this version
        var records = await _context.ReportHistories
            .Where(h => h.ReportId == reportId && h.Version == version)
            .OrderBy(h => h.SequenceNumber)
            .ToListAsync(cancellationToken);
        
        if (!records.Any())
            return null;
        
        // Prioritize content changes (Action = Updated or Updated)
        // These will have Submission field in NewValues
        var contentChange = records.FirstOrDefault(r => 
            r.Action == Domain.Enums.ReportHistoryAction.Updated && 
            !string.IsNullOrEmpty(r.NewValues) && 
            r.NewValues.Contains("Submission"));
        
        // If no content change found, return first sequence (earliest change in version)
        return contentChange ?? records.First();
    }
    
    /// <inheritdoc/>
    public async Task<ReportHistory?> GetVersionBySequenceAsync(Guid reportId, int version, int sequence, CancellationToken cancellationToken = default)
    {
        return await _context.ReportHistories
            .Where(h => h.ReportId == reportId && h.Version == version && h.SequenceNumber == sequence)
            .FirstOrDefaultAsync(cancellationToken);
    }
    
    /// <inheritdoc/>
    public async Task<List<ReportHistory>> GetUserChangesAsync(string userId, DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
    {
        var query = _context.ReportHistories
            .Where(h => h.ChangedBy == userId);
        
        if (from.HasValue)
        {
            query = query.Where(h => h.ChangedAt >= from.Value);
        }
        
        if (to.HasValue)
        {
            query = query.Where(h => h.ChangedAt <= to.Value);
        }
        
        return await query
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync(cancellationToken);
    }
    
    /// <inheritdoc/>
    public async Task<List<ReportHistory>> GetVersionRangeAsync(Guid reportId, int fromVersion, int toVersion, CancellationToken cancellationToken = default)
    {
        return await _context.ReportHistories
            .Where(h => h.ReportId == reportId && h.Version >= fromVersion && h.Version <= toVersion)
            .OrderBy(h => h.Version)
            .ThenBy(h => h.ChangedAt)
            .ToListAsync(cancellationToken);
    }
    
    /// <inheritdoc/>
    public async Task<List<ReportHistory>> GetAllSequencesForVersionAsync(Guid reportId, int version, CancellationToken cancellationToken = default)
    {
        return await _context.ReportHistories
            .Where(h => h.ReportId == reportId && h.Version == version)
            .OrderBy(h => h.SequenceNumber)
            .ToListAsync(cancellationToken);
    }
    
    /// <inheritdoc/>
    public async Task<ReportHistory?> GetLastSequenceForVersionAsync(Guid reportId, int version, CancellationToken cancellationToken = default)
    {
        return await _context.ReportHistories
            .Where(h => h.ReportId == reportId && h.Version == version)
            .OrderByDescending(h => h.SequenceNumber)
            .FirstOrDefaultAsync(cancellationToken);
    }
    
    /// <inheritdoc/>
    public async Task<List<ReportHistory>> GetSequenceRangeAsync(Guid reportId, int version, int fromSequence, int toSequence, CancellationToken cancellationToken = default)
    {
        return await _context.ReportHistories
            .Where(h => h.ReportId == reportId && h.Version == version && h.SequenceNumber >= fromSequence && h.SequenceNumber <= toSequence)
            .OrderBy(h => h.SequenceNumber)
            .ToListAsync(cancellationToken);
    }
}
