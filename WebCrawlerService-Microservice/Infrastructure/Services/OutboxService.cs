using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebCrawlerService.Domain.Interfaces;
using WebCrawlerService.Domain.Common;
using WebCrawlerService.Domain.Entities;
using WebCrawlerService.Infrastructure.Contexts;

namespace WebCrawlerService.Infrastructure.Services;

public class OutboxService : IOutboxService
{
    private readonly CrawlerDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<OutboxService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public OutboxService(
        CrawlerDbContext context,
        IEventPublisher eventPublisher,
        ILogger<OutboxService> logger)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task AddOutboxMessageAsync<T>(T domainEvent, CancellationToken cancellationToken = default) where T : BaseEvent
    {
        var outboxMessage = new OutboxMessage
        {
            Type = typeof(T).Name,
            Content = JsonSerializer.Serialize(domainEvent, _jsonOptions),
            OccurredOnUtc = DateTime.UtcNow
        };

        _context.OutboxMessages.Add(outboxMessage);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Added outbox message {MessageId} of type {EventType}", outboxMessage.Id, outboxMessage.Type);
    }

    public async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken = default)
    {
        var unprocessedMessages = await GetUnprocessedMessagesAsync(50, cancellationToken);

        foreach (var message in unprocessedMessages)
        {
            try
            {
                await _eventPublisher.PublishAsync(message.Type, message.Content, cancellationToken);
                await MarkAsProcessedAsync(message.Id, cancellationToken);
                
                _logger.LogDebug("Successfully processed outbox message {MessageId}", message.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process outbox message {MessageId}", message.Id);
                await MarkAsFailedAsync(message.Id, ex.Message, cancellationToken);
            }
        }
    }

    public async Task<IEnumerable<OutboxMessageDto>> GetUnprocessedMessagesAsync(int batchSize = 50, CancellationToken cancellationToken = default)
    {
        var messages = await _context.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null)
            .Where(m => m.NextRetryAtUtc == null || m.NextRetryAtUtc <= DateTime.UtcNow)
            .Where(m => m.RetryCount < m.MaxRetries)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(batchSize)
            .Select(m => new OutboxMessageDto
            {
                Id = m.Id,
                Type = m.Type,
                Content = m.Content,
                OccurredOnUtc = m.OccurredOnUtc,
                RetryCount = m.RetryCount
            })
            .ToListAsync(cancellationToken);

        return messages;
    }

    public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await _context.OutboxMessages.FindAsync(new object[] { messageId }, cancellationToken);
        if (message != null)
        {
            message.ProcessedOnUtc = DateTime.UtcNow;
            message.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default)
    {
        var message = await _context.OutboxMessages.FindAsync(new object[] { messageId }, cancellationToken);
        if (message != null)
        {
            message.RetryCount++;
            message.Error = error;
            message.UpdatedAt = DateTime.UtcNow;
            
            // Exponential backoff for retries
            var delayMinutes = Math.Pow(2, message.RetryCount);
            message.NextRetryAtUtc = DateTime.UtcNow.AddMinutes(delayMinutes);
            
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}