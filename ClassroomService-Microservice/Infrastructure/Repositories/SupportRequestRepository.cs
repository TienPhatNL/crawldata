using ClassroomService.Domain.Common;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClassroomService.Infrastructure.Repositories;

public class SupportRequestRepository : Repository<SupportRequest>, ISupportRequestRepository
{
    public SupportRequestRepository(ClassroomDbContext context) : base(context)
    {
    }

    public async Task<PagedResult<SupportRequest>> GetPendingSupportRequestsAsync(
        Guid? courseId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SupportRequests
            .Include(sr => sr.Course)
            .Where(sr => sr.Status == SupportRequestStatus.Pending);

        if (courseId.HasValue)
        {
            query = query.Where(sr => sr.CourseId == courseId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(sr => sr.Priority)
            .ThenBy(sr => sr.RequestedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<SupportRequest>.Create(items, totalCount, pageNumber, pageSize);
    }

    public async Task<PagedResult<SupportRequest>> GetMySupportRequestsAsync(
        Guid userId,
        Guid? courseId,
        SupportRequestStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SupportRequests
            .Include(sr => sr.Course)
            .Include(sr => sr.Conversation)
            .Where(sr => sr.RequesterId == userId);

        if (courseId.HasValue)
        {
            query = query.Where(sr => sr.CourseId == courseId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(sr => sr.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(sr => sr.RequestedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<SupportRequest>.Create(items, totalCount, pageNumber, pageSize);
    }

    public async Task<PagedResult<SupportRequest>> GetStaffSupportRequestsAsync(
        Guid staffId,
        SupportRequestStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SupportRequests
            .Include(sr => sr.Course)
            .Include(sr => sr.Conversation)
            .Where(sr => sr.AssignedStaffId == staffId);

        if (status.HasValue)
        {
            query = query.Where(sr => sr.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(sr => sr.Priority)
            .ThenByDescending(sr => sr.AcceptedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<SupportRequest>.Create(items, totalCount, pageNumber, pageSize);
    }

    public async Task<SupportRequest?> GetSupportRequestByIdAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        return await _context.SupportRequests
            .Include(sr => sr.Course)
            .Include(sr => sr.Conversation)
            .FirstOrDefaultAsync(sr => sr.Id == requestId, cancellationToken);
    }

    public async Task<SupportRequest?> GetActiveRequestForUserInCourseAsync(
        Guid userId,
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        return await _context.SupportRequests
            .FirstOrDefaultAsync(
                sr => sr.RequesterId == userId
                      && sr.CourseId == courseId
                      && (sr.Status == SupportRequestStatus.Pending
                          || sr.Status == SupportRequestStatus.InProgress),
                cancellationToken);
    }
}
