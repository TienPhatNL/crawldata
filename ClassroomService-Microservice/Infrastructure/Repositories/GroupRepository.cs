using Microsoft.EntityFrameworkCore;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Infrastructure.Persistence;

namespace ClassroomService.Infrastructure.Repositories;

public class GroupRepository : Repository<Group>, IGroupRepository
{
    public GroupRepository(ClassroomDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Group>> GetGroupsByCourseAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(g => g.CourseId == courseId)
            .Include(g => g.Course)
                .ThenInclude(c => c.CourseCode)
            .Include(g => g.Course)
                .ThenInclude(c => c.Term)
            .Include(g => g.Assignment)
            .Include(g => g.Members)
                .ThenInclude(m => m.Enrollment)
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Group>> GetGroupsByAssignmentAsync(Guid assignmentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(g => g.AssignmentId == assignmentId)
            .Include(g => g.Course)
                .ThenInclude(c => c.CourseCode)
            .Include(g => g.Course)
                .ThenInclude(c => c.Term)
            .Include(g => g.Assignment)
            .Include(g => g.Members)
                .ThenInclude(m => m.Enrollment)
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Group>> GetStudentGroupsInCourseAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(g => g.CourseId == courseId && g.Members.Any(m => m.Enrollment.StudentId == studentId))
            .Include(g => g.Course)
                .ThenInclude(c => c.CourseCode)
            .Include(g => g.Course)
                .ThenInclude(c => c.Term)
            .Include(g => g.Assignment)
            .Include(g => g.Members)
                .ThenInclude(m => m.Enrollment)
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Group?> GetGroupWithMembersAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(g => g.Course)
                .ThenInclude(c => c.CourseCode)
            .Include(g => g.Course)
                .ThenInclude(c => c.Term)
            .Include(g => g.Assignment)
            .Include(g => g.Members)
                .ThenInclude(m => m.Enrollment)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);
    }

    public async Task<IEnumerable<Group>> GetAvailableGroupsAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(g => g.CourseId == courseId && 
                        !g.IsLocked && 
                        (!g.MaxMembers.HasValue || g.Members.Count < g.MaxMembers.Value))
            .Include(g => g.Course)
                .ThenInclude(c => c.CourseCode)
            .Include(g => g.Course)
                .ThenInclude(c => c.Term)
            .Include(g => g.Assignment)
            .Include(g => g.Members)
                .ThenInclude(m => m.Enrollment)
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> GroupNameExistsAsync(Guid courseId, string groupName, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(g => g.CourseId == courseId && g.Name == groupName, cancellationToken);
    }

    public async Task<Group?> GetGroupByNameAsync(Guid courseId, string groupName, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(g => g.Course)
                .ThenInclude(c => c.CourseCode)
            .Include(g => g.Course)
                .ThenInclude(c => c.Term)
            .Include(g => g.Assignment)
            .Include(g => g.Members)
                .ThenInclude(m => m.Enrollment)
            .FirstOrDefaultAsync(g => g.CourseId == courseId && g.Name == groupName, cancellationToken);
    }
}
