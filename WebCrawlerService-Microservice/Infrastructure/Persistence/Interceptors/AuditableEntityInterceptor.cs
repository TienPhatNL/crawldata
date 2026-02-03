using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WebCrawlerService.Domain.Interfaces;
using WebCrawlerService.Domain.Common;

namespace WebCrawlerService.Infrastructure.Persistence.Interceptors;

public class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public AuditableEntityInterceptor(
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService)
    {
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateEntities(DbContext? context)
    {
        //if (context == null) return;

        //var utcNow = _dateTimeService.UtcNow;
        //var currentUserId = _currentUserService.UserId;

        //foreach (var entry in context.ChangeTracker.Entries<BaseAuditableEntity>())
        //{
        //    switch (entry.State)
        //    {
        //        case EntityState.Added:
        //            entry.Entity.CreatedBy = currentUserId;
        //            entry.Entity.CreatedAt = utcNow;
        //            break;

        //        case EntityState.Modified:
        //            entry.Entity.LastModifiedBy = currentUserId;
        //            entry.Entity.LastModifiedAt = utcNow;
        //            break;

        //        case EntityState.Deleted:
        //            if (entry.Entity is ISoftDelete softDeleteEntity)
        //            {
        //                softDeleteEntity.DeletedBy = currentUserId;
        //                softDeleteEntity.DeletedAt = utcNow;
        //                softDeleteEntity.IsDeleted = true;
        //                entry.State = EntityState.Modified;
        //            }
        //            break;
        //    }
        //}

        //// Handle entities that only implement BaseEntity (not BaseAuditableEntity)
        //foreach (var entry in context.ChangeTracker.Entries<BaseEntity>()
        //             .Where(e => e.Entity is not BaseAuditableEntity))
        //{
        //    switch (entry.State)
        //    {
        //        case EntityState.Added:
        //            entry.Entity.CreatedAt = utcNow;
        //            break;

        //        case EntityState.Modified:
        //            entry.Entity.UpdatedAt = utcNow;
        //            break;
        //    }
        //}
    }
}