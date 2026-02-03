using Microsoft.EntityFrameworkCore;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Infrastructure.Persistence;

namespace ClassroomService.Infrastructure.Repositories;

public class CourseRepository : Repository<Course>, ICourseRepository
{
    public CourseRepository(ClassroomDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Course>> GetCoursesByLecturerAsync(Guid lecturerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.LecturerId == lecturerId)
            .Include(c => c.CourseCode)
            .Include(c => c.Term)
            .Include(c => c.Enrollments)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Course>> GetCoursesByTermAsync(Guid termId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.TermId == termId)
            .Include(c => c.CourseCode)
            .Include(c => c.Term)
            .Include(c => c.Enrollments)
            .OrderBy(c => c.CourseCode.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<Course?> GetCourseByAccessCodeAsync(string accessCode, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.CourseCode)
            .Include(c => c.Term)
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(c => c.AccessCode == accessCode && c.RequiresAccessCode, cancellationToken);
    }

    public async Task<Course?> GetCourseByUniqueCodeAsync(string uniqueCode, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.CourseCode)
            .Include(c => c.Term)
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(c => c.UniqueCode == uniqueCode, cancellationToken);
    }

    public async Task<Course?> GetCourseWithEnrollmentsAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Enrollments.Where(e => e.Status == EnrollmentStatus.Active))
            .Include(c => c.CourseCode)
            .Include(c => c.Term)
            .FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken);
    }

    public async Task<IEnumerable<Course>> GetCoursesByStatusAsync(CourseStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.Status == status)
            .Include(c => c.CourseCode)
            .Include(c => c.Term)
            .Include(c => c.Enrollments)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Course>> GetActiveCoursesForTermAsync(Guid termId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.TermId == termId && c.Status == CourseStatus.Active)
            .Include(c => c.CourseCode)
            .Include(c => c.Term)
            .Include(c => c.Enrollments)
            .OrderBy(c => c.CourseCode.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<Course?> GetCourseWithDetailsAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.CourseCode)
            .Include(c => c.Term)
            .Include(c => c.Enrollments)
            .Include(c => c.Assignments)
            .Include(c => c.Groups)
                .ThenInclude(g => g.Members)
            .FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken);
    }

    public async Task<IEnumerable<Course>> GetCoursesByLecturerIdAsync(Guid lecturerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.LecturerId == lecturerId)
            .Include(c => c.CourseCode)
            .Include(c => c.Term)
            .Include(c => c.Enrollments)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
