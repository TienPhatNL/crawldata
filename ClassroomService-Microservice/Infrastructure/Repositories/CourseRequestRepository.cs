using Microsoft.EntityFrameworkCore;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Infrastructure.Persistence;

namespace ClassroomService.Infrastructure.Repositories;

public class CourseRequestRepository : Repository<CourseRequest>, ICourseRequestRepository
{
    public CourseRequestRepository(ClassroomDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<CourseRequest>> GetPendingRequestsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(cr => cr.Status == CourseRequestStatus.Pending)
            .Include(cr => cr.CourseCode)
            .Include(cr => cr.Term)
            .OrderBy(cr => cr.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CourseRequest>> GetRequestsByLecturerAsync(Guid lecturerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(cr => cr.LecturerId == lecturerId)
            .Include(cr => cr.CourseCode)
            .Include(cr => cr.Term)
            .OrderByDescending(cr => cr.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CourseRequest>> GetRequestsByStatusAsync(CourseRequestStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(cr => cr.Status == status)
            .Include(cr => cr.CourseCode)
            .Include(cr => cr.Term)
            .OrderByDescending(cr => cr.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<CourseRequest?> GetRequestWithDetailsAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(cr => cr.CourseCode)
            .Include(cr => cr.Term)
            .Include(cr => cr.CreatedCourse)
            .FirstOrDefaultAsync(cr => cr.Id == requestId, cancellationToken);
    }

    public async Task<IEnumerable<CourseRequest>> GetRequestsByTermAsync(Guid termId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(cr => cr.TermId == termId)
            .Include(cr => cr.CourseCode)
            .Include(cr => cr.Term)
            .OrderByDescending(cr => cr.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> SimilarRequestExistsAsync(Guid lecturerId, Guid courseCodeId, Guid termId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(cr => cr.LecturerId == lecturerId && 
                           cr.CourseCodeId == courseCodeId && 
                           cr.TermId == termId && 
                           cr.Status == CourseRequestStatus.Pending, 
                      cancellationToken);
    }
}
