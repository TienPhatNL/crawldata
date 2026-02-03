using Microsoft.EntityFrameworkCore;
using WebCrawlerService.Domain.Common;

namespace WebCrawlerService.Infrastructure.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<T> WhereNotDeleted<T>(this IQueryable<T> query) where T : class, ISoftDelete
    {
        return query.Where(x => !x.IsDeleted);
    }

    public static IQueryable<T> IncludeDeleted<T>(this IQueryable<T> query) where T : class, ISoftDelete
    {
        return query.IgnoreQueryFilters();
    }

    public static IQueryable<T> WhereDeleted<T>(this IQueryable<T> query) where T : class, ISoftDelete
    {
        return query.IgnoreQueryFilters().Where(x => x.IsDeleted);
    }
}