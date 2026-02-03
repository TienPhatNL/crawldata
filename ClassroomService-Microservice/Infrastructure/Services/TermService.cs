using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClassroomService.Infrastructure.Services;

public class TermService : ITermService
{
    private readonly ClassroomDbContext _context;
    
    public TermService(ClassroomDbContext context)
    {
        _context = context;
    }
    
    public async Task<bool> IsTermActiveAsync(Guid termId)
    {
        var now = DateTime.UtcNow;
        return await _context.Terms
            .AnyAsync(t => t.Id == termId && 
                          t.IsActive && 
                          t.StartDate <= now && 
                          t.EndDate >= now);
    }
    
    public async Task<bool> IsTermPastAsync(Guid termId)
    {
        var now = DateTime.UtcNow;
        return await _context.Terms
            .AnyAsync(t => t.Id == termId && 
                          t.IsActive && 
                          t.EndDate < now);
    }
    
    public async Task<List<Term>> GetActiveTermsAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.Terms
            .Where(t => t.IsActive && 
                       t.StartDate <= now && 
                       t.EndDate >= now)
            .ToListAsync();
    }
    
    public async Task<bool> HasActiveTermForCourseCodeAsync(Guid courseCodeId)
    {
        var now = DateTime.UtcNow;
        
        // Check if ANY course with this CourseCodeId is in an active term
        return await _context.Courses
            .Include(c => c.Term)
            .AnyAsync(c => c.CourseCodeId == courseCodeId &&
                          c.Term.IsActive &&
                          c.Term.StartDate <= now &&
                          c.Term.EndDate >= now);
    }
    
    public async Task<bool> HasActiveTermForCourseAsync(Guid courseId)
    {
        var now = DateTime.UtcNow;
        
        return await _context.Courses
            .Include(c => c.Term)
            .AnyAsync(c => c.Id == courseId &&
                          c.Term.IsActive &&
                          c.Term.StartDate <= now &&
                          c.Term.EndDate >= now);
    }
    
    public async Task<bool> HasPastOrActiveTermForCourseCodeAsync(Guid courseCodeId)
    {
        var now = DateTime.UtcNow;
        
        // Check if ANY course with this CourseCodeId is in a PAST or ACTIVE term
        return await _context.Courses
            .Include(c => c.Term)
            .AnyAsync(c => c.CourseCodeId == courseCodeId &&
                          c.Term.IsActive &&
                          c.Term.StartDate <= now); // Started already = past or active
    }
    
    public async Task<bool> HasPastOrActiveTermForCourseAsync(Guid courseId)
    {
        var now = DateTime.UtcNow;
        
        return await _context.Courses
            .Include(c => c.Term)
            .AnyAsync(c => c.Id == courseId &&
                          c.Term.IsActive &&
                          c.Term.StartDate <= now); // Started already = past or active
    }
    
    public async Task<List<string>> GetAffectedTermNamesAsync(Guid? courseCodeId, Guid? courseId)
    {
        var now = DateTime.UtcNow;
        
        if (courseCodeId.HasValue)
        {
            // Get all terms for courses with this course code
            return await _context.Courses
                .Include(c => c.Term)
                .Where(c => c.CourseCodeId == courseCodeId.Value &&
                           c.Term.IsActive &&
                           c.Term.StartDate <= now &&
                           c.Term.EndDate >= now)
                .Select(c => c.Term.Name)
                .Distinct()
                .ToListAsync();
        }
        else if (courseId.HasValue)
        {
            // Get term for specific course
            var term = await _context.Courses
                .Include(c => c.Term)
                .Where(c => c.Id == courseId.Value &&
                           c.Term.IsActive &&
                           c.Term.StartDate <= now &&
                           c.Term.EndDate >= now)
                .Select(c => c.Term.Name)
                .FirstOrDefaultAsync();
            
            return term != null ? new List<string> { term } : new List<string>();
        }
        
        return new List<string>();
    }
    
    public async Task<List<Guid>> GetAffectedTermIdsAsync(Guid? courseCodeId, Guid? courseId)
    {
        var now = DateTime.UtcNow;
        
        if (courseCodeId.HasValue)
        {
            // Get all term IDs for courses with this course code
            return await _context.Courses
                .Include(c => c.Term)
                .Where(c => c.CourseCodeId == courseCodeId.Value &&
                           c.Term.IsActive &&
                           c.Term.StartDate <= now &&
                           c.Term.EndDate >= now)
                .Select(c => c.Term.Id)
                .Distinct()
                .ToListAsync();
        }
        else if (courseId.HasValue)
        {
            // Get term ID for specific course
            var termId = await _context.Courses
                .Include(c => c.Term)
                .Where(c => c.Id == courseId.Value &&
                           c.Term.IsActive &&
                           c.Term.StartDate <= now &&
                           c.Term.EndDate >= now)
                .Select(c => c.Term.Id)
                .FirstOrDefaultAsync();
            
            return termId != Guid.Empty ? new List<Guid> { termId } : new List<Guid>();
        }
        
        return new List<Guid>();
    }
    
    public async Task<Term?> GetCourseTermAsync(Guid courseId)
    {
        return await _context.Courses
            .Include(c => c.Term)
            .Where(c => c.Id == courseId)
            .Select(c => c.Term)
            .FirstOrDefaultAsync();
    }
}
