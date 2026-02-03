using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WebCrawlerService.Domain.Interfaces;
using WebCrawlerService.Domain.Common;

namespace WebCrawlerService.Infrastructure.Persistence.Interceptors;

public class OutboxInterceptor : SaveChangesInterceptor
{
    private readonly IOutboxService _outboxService;

    public OutboxInterceptor(IOutboxService outboxService)
    {
        _outboxService = outboxService;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            await ConvertDomainEventsToOutboxMessages(eventData.Context, cancellationToken);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            ConvertDomainEventsToOutboxMessages(eventData.Context, CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        }

        return base.SavingChanges(eventData, result);
    }

    private async Task ConvertDomainEventsToOutboxMessages(DbContext context, CancellationToken cancellationToken)
    {
        var domainEntities = context.ChangeTracker
            .Entries<BaseEntity>()
            .Where(x => x.Entity.DomainEvents.Any())
            .Select(x => x.Entity)
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(x => x.DomainEvents)
            .ToList();

        domainEntities.ForEach(entity => entity.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
        {
            await _outboxService.AddOutboxMessageAsync(domainEvent, cancellationToken);
        }
    }
}