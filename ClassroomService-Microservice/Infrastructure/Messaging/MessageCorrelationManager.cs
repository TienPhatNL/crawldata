using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace ClassroomService.Infrastructure.Messaging;

/// <summary>
/// Manages correlation between async Kafka requests and responses
/// </summary>
public class MessageCorrelationManager
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<object>> _pendingRequests = new();
    private readonly ILogger<MessageCorrelationManager> _logger;

    public MessageCorrelationManager(ILogger<MessageCorrelationManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Register a pending request and return a task that completes when response is received
    /// </summary>
    public Task<TResponse> RegisterRequest<TResponse>(string correlationId, TimeSpan timeout)
    {
        var tcs = new TaskCompletionSource<object>();
        
        if (!_pendingRequests.TryAdd(correlationId, tcs))
        {
            _logger.LogWarning("Correlation ID {CorrelationId} already exists", correlationId);
            throw new InvalidOperationException($"Correlation ID {correlationId} already exists");
        }

        _logger.LogDebug("Registered request with correlation ID: {CorrelationId}", correlationId);

        // Set timeout to cancel the request
        _ = Task.Delay(timeout).ContinueWith(t =>
        {
            if (_pendingRequests.TryRemove(correlationId, out var pending))
            {
                _logger.LogWarning("Request with correlation ID {CorrelationId} timed out after {Timeout}", 
                    correlationId, timeout);
                pending.TrySetException(new TimeoutException(
                    $"Request {correlationId} timed out after {timeout.TotalSeconds} seconds"));
            }
        });

        return tcs.Task.ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                _logger.LogError(t.Exception, "Request with correlation ID {CorrelationId} failed", correlationId);
                throw t.Exception!.InnerException ?? t.Exception;
            }
            return (TResponse)t.Result;
        });
    }

    /// <summary>
    /// Complete a pending request with the response
    /// </summary>
    public void CompleteRequest(string correlationId, object response)
    {
        if (_pendingRequests.TryRemove(correlationId, out var tcs))
        {
            _logger.LogDebug("Completed request with correlation ID: {CorrelationId}", correlationId);
            tcs.TrySetResult(response);
        }
        else
        {
            _logger.LogWarning("Received response for unknown or expired correlation ID: {CorrelationId}", 
                correlationId);
        }
    }

    /// <summary>
    /// Cancel a pending request
    /// </summary>
    public void CancelRequest(string correlationId, string reason)
    {
        if (_pendingRequests.TryRemove(correlationId, out var tcs))
        {
            _logger.LogWarning("Cancelled request with correlation ID {CorrelationId}: {Reason}", 
                correlationId, reason);
            tcs.TrySetException(new OperationCanceledException(reason));
        }
    }

    /// <summary>
    /// Get count of pending requests
    /// </summary>
    public int PendingRequestCount => _pendingRequests.Count;
}
