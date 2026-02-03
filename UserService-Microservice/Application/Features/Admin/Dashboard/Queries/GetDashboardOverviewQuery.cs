using System.Net;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using UserService.Application.Common.Models;
using UserService.Application.Features.Admin.Dashboard.DTOs;

namespace UserService.Application.Features.Admin.Dashboard.Queries;

public class GetDashboardOverviewQuery : IRequest<ResponseModel>
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Interval { get; set; } = "day";
}

public class GetDashboardOverviewQueryHandler : IRequestHandler<GetDashboardOverviewQuery, ResponseModel>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDistributedCache _cache;

    public GetDashboardOverviewQueryHandler(IServiceScopeFactory scopeFactory, IDistributedCache cache)
    {
        _scopeFactory = scopeFactory;
        _cache = cache;
    }

    public async Task<ResponseModel> Handle(GetDashboardOverviewQuery request, CancellationToken cancellationToken)
    {
        var endDate = request.EndDate ?? DateTime.UtcNow;
        var startDate = request.StartDate ?? endDate.AddDays(-30);

        // Check cache
        var cacheKey = $"admin:dashboard:overview:{startDate:yyyy-MM-dd}:{endDate:yyyy-MM-dd}:{request.Interval}";
        var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
        
        if (!string.IsNullOrEmpty(cachedData))
        {
            var cachedResult = JsonSerializer.Deserialize<DashboardOverviewDto>(cachedData);
            return new ResponseModel
            {
                Status = HttpStatusCode.OK,
                Data = cachedResult
            };
        }

        // Execute all queries in parallel with separate scopes to avoid DbContext threading issues
        var revenueTask = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            return await mediator.Send(new GetRevenueStatisticsQuery
            {
                StartDate = startDate,
                EndDate = endDate,
                Interval = request.Interval
            }, cancellationToken);
        }, cancellationToken);

        var paymentsTask = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            return await mediator.Send(new GetPaymentStatisticsQuery
            {
                StartDate = startDate,
                EndDate = endDate,
                Interval = request.Interval
            }, cancellationToken);
        }, cancellationToken);

        var subscriptionsTask = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            return await mediator.Send(new GetSubscriptionStatisticsQuery
            {
                StartDate = startDate,
                EndDate = endDate
            }, cancellationToken);
        }, cancellationToken);

        var usersTask = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            return await mediator.Send(new GetUserStatisticsQuery
            {
                StartDate = startDate,
                EndDate = endDate
            }, cancellationToken);
        }, cancellationToken);

        await Task.WhenAll(revenueTask, paymentsTask, subscriptionsTask, usersTask);

        var overview = new DashboardOverviewDto
        {
            Period = new PeriodInfo
            {
                StartDate = startDate,
                EndDate = endDate,
                Description = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}"
            },
            Revenue = (RevenueStatisticsDto)revenueTask.Result.Data!,
            Payments = (PaymentStatisticsDto)paymentsTask.Result.Data!,
            Subscriptions = (SubscriptionStatisticsDto)subscriptionsTask.Result.Data!,
            Users = (UserStatisticsDto)usersTask.Result.Data!,
            GeneratedAt = DateTime.UtcNow
        };

        // Cache for 5 minutes (shorter TTL since it aggregates multiple queries)
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(overview), cacheOptions, cancellationToken);

        return new ResponseModel(HttpStatusCode.OK, "Dashboard overview retrieved successfully", overview);
    }
}
