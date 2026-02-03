using Microsoft.EntityFrameworkCore;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Infrastructure.Persistence;

namespace ClassroomService.Infrastructure.Repositories;

public class GroupMemberRepository : Repository<GroupMember>, IGroupMemberRepository
{
    public GroupMemberRepository(ClassroomDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<GroupMember>> GetMembersByGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(m => m.GroupId == groupId)
            .Include(m => m.Enrollment)
            .Include(m => m.Group)
                .ThenInclude(g => g.Course)
                    .ThenInclude(c => c.CourseCode)
            .Include(m => m.Group)
                .ThenInclude(g => g.Course)
                    .ThenInclude(c => c.Term)
            .Include(m => m.Group)
                .ThenInclude(g => g.Assignment)
            .OrderByDescending(m => m.IsLeader)
            .ThenBy(m => m.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<GroupMember?> GetGroupLeaderAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(m => m.Enrollment)
            .Include(m => m.Group)
                .ThenInclude(g => g.Course)
                    .ThenInclude(c => c.CourseCode)
            .Include(m => m.Group)
                .ThenInclude(g => g.Course)
                    .ThenInclude(c => c.Term)
            .Include(m => m.Group)
                .ThenInclude(g => g.Assignment)
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.IsLeader, cancellationToken);
    }

    public async Task<bool> IsStudentInGroupAsync(Guid groupId, Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(m => m.Enrollment)
            .AnyAsync(m => m.GroupId == groupId && m.Enrollment.StudentId == studentId, cancellationToken);
    }

    public async Task<GroupMember?> GetGroupMemberAsync(Guid groupId, Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(m => m.Enrollment)
            .Include(m => m.Group)
                .ThenInclude(g => g.Course)
                    .ThenInclude(c => c.CourseCode)
            .Include(m => m.Group)
                .ThenInclude(g => g.Course)
                    .ThenInclude(c => c.Term)
            .Include(m => m.Group)
                .ThenInclude(g => g.Assignment)
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.Enrollment.StudentId == studentId, cancellationToken);
    }

    public async Task<IEnumerable<GroupMember>> GetStudentGroupsAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(m => m.Enrollment)
            .Where(m => m.Enrollment.StudentId == studentId)
            .Include(m => m.Group)
                .ThenInclude(g => g.Course)
                    .ThenInclude(c => c.CourseCode)
            .Include(m => m.Group)
                .ThenInclude(g => g.Course)
                    .ThenInclude(c => c.Term)
            .Include(m => m.Group)
                .ThenInclude(g => g.Assignment)
            .Include(m => m.Group)
                .ThenInclude(g => g.Members)
                    .ThenInclude(m => m.Enrollment)
            .OrderByDescending(m => m.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetMemberCountAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .CountAsync(m => m.GroupId == groupId, cancellationToken);
    }

    public async Task<IEnumerable<GroupMember>> GetMembersByRoleAsync(Guid groupId, GroupMemberRole role, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(m => m.GroupId == groupId && m.Role == role)
            .Include(m => m.Enrollment)
            .Include(m => m.Group)
                .ThenInclude(g => g.Course)
                    .ThenInclude(c => c.CourseCode)
            .Include(m => m.Group)
                .ThenInclude(g => g.Course)
                    .ThenInclude(c => c.Term)
            .Include(m => m.Group)
                .ThenInclude(g => g.Assignment)
            .OrderBy(m => m.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<GroupMember>> GetMembersByCourseAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(m => m.Enrollment)
            .Include(m => m.Group)
                .ThenInclude(g => g.Course)
                    .ThenInclude(c => c.CourseCode)
            .Include(m => m.Group)
                .ThenInclude(g => g.Course)
                    .ThenInclude(c => c.Term)
            .Include(m => m.Group)
                .ThenInclude(g => g.Assignment)
            .Where(m => m.Group.CourseId == courseId)
            .ToListAsync(cancellationToken);
    }
}
