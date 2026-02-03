namespace CrawlData.ApiGateway.Middleware;

public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if correlation ID already exists in the request headers
        if (!context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId) || 
            string.IsNullOrEmpty(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
            context.Request.Headers[CorrelationIdHeaderName] = correlationId;
        }

        // Add correlation ID to the response headers
        context.Response.Headers[CorrelationIdHeaderName] = correlationId;

        // Add correlation ID to the logging scope
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId.ToString()
        });

        // Store correlation ID in HttpContext items for easy access by other middleware
        context.Items["CorrelationId"] = correlationId.ToString();

        _logger.LogDebug("Processing request with correlation ID: {CorrelationId}", correlationId);

        await _next(context);
    }
}