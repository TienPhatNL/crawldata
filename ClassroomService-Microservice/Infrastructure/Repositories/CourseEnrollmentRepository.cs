using Microsoft.EntityFrameworkCore;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Infrastructure.Persistence;

namespace ClassroomService.Infrastructure.Repositories;

public class CourseEnrollmentRepository : Repository<CourseEnrollment>, ICourseEnrollmentRepository
{
    public CourseEnrollmentRepository(ClassroomDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<CourseEnrollment>> GetEnrollmentsByCourseAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(e => e.CourseId == courseId)
            .Include(e => e.Course)
                .ThenInclude(c => c.CourseCode)
            .Include(e => e.Course)
                .ThenInclude(c => c.Term)
            .OrderBy(e => e.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CourseEnrollment>> GetEnrollmentsByStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(e => e.StudentId == studentId)
            .Include(e => e.Course)
                .ThenInclude(c => c.CourseCode)
            .Include(e => e.Course)
                .ThenInclude(c => c.Term)
            .OrderByDescending(e => e.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CourseEnrollment>> GetActiveEnrollmentsAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Active)
            .Include(e => e.Course)
                .ThenInclude(c => c.CourseCode)
            .Include(e => e.Course)
                .ThenInclude(c => c.Term)
            .OrderByDescending(e => e.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsStudentEnrolledAsync(Guid courseId, Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(e => e.CourseId == courseId && e.StudentId == studentId && e.Status == EnrollmentStatus.Active, cancellationToken);
    }

    public async Task<CourseEnrollment?> GetEnrollmentAsync(Guid courseId, Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(e => e.Course)
                .ThenInclude(c => c.CourseCode)
            .Include(e => e.Course)
                .ThenInclude(c => c.Term)
            .FirstOrDefaultAsync(e => e.CourseId == courseId && e.StudentId == studentId, cancellationToken);
    }

    public async Task<IEnumerable<CourseEnrollment>> GetEnrollmentsByStatusAsync(EnrollmentStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(e => e.Status == status)
            .Include(e => e.Course)
                .ThenInclude(c => c.CourseCode)
            .Include(e => e.Course)
                .ThenInclude(c => c.Term)
            .OrderByDescending(e => e.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetActiveEnrollmentCountAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .CountAsync(e => e.CourseId == courseId && e.Status == EnrollmentStatus.Active, cancellationToken);
    }
}
