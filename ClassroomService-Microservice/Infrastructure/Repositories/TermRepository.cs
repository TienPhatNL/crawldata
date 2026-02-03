using Microsoft.EntityFrameworkCore;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Infrastructure.Persistence;

namespace ClassroomService.Infrastructure.Repositories;

public class TermRepository : Repository<Term>, ITermRepository
{
    public TermRepository(ClassroomDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Term>> GetActiveTermsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Term?> GetTermByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(t => t.Name == name, cancellationToken);
    }

    public async Task<bool> TermNameExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(t => t.Name == name, cancellationToken);
    }

    public async Task<Term?> GetCurrentTermAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.IsActive)
            .OrderBy(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Term?> GetTermWithCoursesAsync(Guid termId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.Courses)
                .ThenInclude(c => c.CourseCode)
            .FirstOrDefaultAsync(t => t.Id == termId, cancellationToken);
    }

    public async Task<Term?> GetOverlappingTermAsync(DateTime startDate, DateTime endDate, Guid? excludeTermId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        // Exclude the specified term if provided (for updates)
        if (excludeTermId.HasValue)
        {
            query = query.Where(t => t.Id != excludeTermId.Value);
        }

        // Check for overlap:
        // Two date ranges overlap if:
        // - startDate < other.EndDate AND endDate > other.StartDate
        return await query
            .Where(t => startDate < t.EndDate && endDate > t.StartDate)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
