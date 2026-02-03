using Microsoft.EntityFrameworkCore;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Infrastructure.Persistence;

namespace ClassroomService.Infrastructure.Repositories
{
    public class AssignmentRepository : Repository<Assignment>, IAssignmentRepository
    {
        public AssignmentRepository(ClassroomDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Assignment>> GetAssignmentsByCourseAsync(Guid courseId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(a => a.CourseId == courseId)
                .Include(a => a.Course)
                    .ThenInclude(c => c.CourseCode)
                .Include(a => a.Course)
                    .ThenInclude(c => c.Term)
                .Include(a => a.AssignedGroups)
                    .ThenInclude(g => g.Members)
                .Include(a => a.Topic)
                .OrderBy(a => a.DueDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Assignment>> GetUpcomingAssignmentsAsync(Guid courseId, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            return await _dbSet
                .Where(a => a.CourseId == courseId && a.DueDate > now && a.Status == AssignmentStatus.Active)
                .Include(a => a.Course)
                    .ThenInclude(c => c.CourseCode)
                .Include(a => a.Course)
                    .ThenInclude(c => c.Term)
                .Include(a => a.AssignedGroups)
                    .ThenInclude(g => g.Members)
                .Include(a => a.Topic)
                .OrderBy(a => a.DueDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Assignment>> GetOverdueAssignmentsAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            return await _dbSet
                .Where(a => a.DueDate < now && a.Status == AssignmentStatus.Active)
                .Include(a => a.Course)
                    .ThenInclude(c => c.CourseCode)
                .Include(a => a.Course)
                    .ThenInclude(c => c.Term)
                .Include(a => a.AssignedGroups)
                    .ThenInclude(g => g.Members)
                .Include(a => a.Topic)
                .OrderBy(a => a.DueDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Assignment>> GetAssignmentsByStatusAsync(AssignmentStatus status, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(a => a.Status == status)
                .Include(a => a.Course)
                    .ThenInclude(c => c.CourseCode)
                .Include(a => a.Course)
                    .ThenInclude(c => c.Term)
                .Include(a => a.AssignedGroups)
                    .ThenInclude(g => g.Members)
                .Include(a => a.Topic)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<Assignment?> GetAssignmentWithGroupsAsync(Guid assignmentId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(a => a.Course)
                    .ThenInclude(c => c.CourseCode)
                .Include(a => a.Course)
                    .ThenInclude(c => c.Term)
                .Include(a => a.AssignedGroups)
                    .ThenInclude(g => g.Members)
                .Include(a => a.AssignedGroups)
                    .ThenInclude(g => g.Course)
                .Include(a => a.Topic)
                .FirstOrDefaultAsync(a => a.Id == assignmentId, cancellationToken);
        }

        public async Task<IEnumerable<Assignment>> GetActiveAssignmentsAsync(Guid courseId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(a => a.CourseId == courseId && a.Status != AssignmentStatus.Closed)
                .Include(a => a.Course)
                    .ThenInclude(c => c.CourseCode)
                .Include(a => a.Course)
                    .ThenInclude(c => c.Term)
                .Include(a => a.AssignedGroups)
                    .ThenInclude(g => g.Members)
                .Include(a => a.Topic)
                .OrderBy(a => a.DueDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Assignment>> GetAssignmentsToOpenAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            return await _dbSet
                .Where(a => a.Status == AssignmentStatus.Draft &&
                            a.StartDate.HasValue &&
                            a.StartDate.Value <= now)
                .Include(a => a.Course)
                    .ThenInclude(c => c.CourseCode)
                .Include(a => a.Course)
                    .ThenInclude(c => c.Term)
                .Include(a => a.AssignedGroups)
                    .ThenInclude(g => g.Members)
                .Include(a => a.Topic)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Assignment>> GetAssignmentsToCloseAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            return await _dbSet
                .Where(a => a.Status == AssignmentStatus.Active &&
                            ((a.ExtendedDueDate.HasValue && a.ExtendedDueDate.Value <= now) ||
                             (!a.ExtendedDueDate.HasValue && a.DueDate <= now)))
                .Include(a => a.Course)
                    .ThenInclude(c => c.CourseCode)
                .Include(a => a.Course)
                    .ThenInclude(c => c.Term)
                .Include(a => a.AssignedGroups)
                    .ThenInclude(g => g.Members)
                .Include(a => a.Topic)
                .ToListAsync(cancellationToken);
        }
    }
}