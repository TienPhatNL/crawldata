using Microsoft.EntityFrameworkCore;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Infrastructure.Persistence;

namespace ClassroomService.Infrastructure.Repositories;

public class CourseCodeRepository : Repository<CourseCode>, ICourseCodeRepository
{
    public CourseCodeRepository(ClassroomDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<CourseCode>> GetActiveCourseCodesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(cc => cc.IsActive)
            .Include(cc => cc.Courses)
                .ThenInclude(c => c.Term)
            .OrderBy(cc => cc.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<CourseCode?> GetCourseCodeByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(cc => cc.Courses)
                .ThenInclude(c => c.Term)
            .FirstOrDefaultAsync(cc => cc.Code == code, cancellationToken);
    }

    public async Task<IEnumerable<CourseCode>> GetCourseCodesByDepartmentAsync(string department, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(cc => cc.Department == department)
            .Include(cc => cc.Courses)
                .ThenInclude(c => c.Term)
            .OrderBy(cc => cc.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> CourseCodeExistsAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(cc => cc.Code == code, cancellationToken);
    }

    public async Task<CourseCode?> GetCourseCodeWithCoursesAsync(Guid courseCodeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(cc => cc.Courses)
                .ThenInclude(c => c.Term)
            .FirstOrDefaultAsync(cc => cc.Id == courseCodeId, cancellationToken);
    }
}
