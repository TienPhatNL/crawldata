using MediatR;
using Microsoft.Extensions.Logging;
using WebCrawlerService.Application.Common.Interfaces;

namespace WebCrawlerService.Application.Common.Behaviors;

public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICacheableRequest<TResponse>
    where TResponse : class
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(ICacheService cacheService, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request.BypassCache)
        {
            return await next();
        }

        var cachedResponse = await _cacheService.GetAsync<TResponse>(request.CacheKey, cancellationToken);
        if (cachedResponse != null)
        {
            _logger.LogDebug("Cache hit for key: {CacheKey}", request.CacheKey);
            return cachedResponse;
        }

        _logger.LogDebug("Cache miss for key: {CacheKey}", request.CacheKey);
        var response = await next();

        if (response != null)
        {
            await _cacheService.SetAsync(request.CacheKey, response, request.Expiry, cancellationToken);
            _logger.LogDebug("Response cached for key: {CacheKey}", request.CacheKey);
        }

        return response;
    }
}

public interface ICacheableRequest<out T> : IRequest<T>
{
    string CacheKey { get; }
    TimeSpan? Expiry { get; }
    bool BypassCache { get; }
}