using System.Net;
using System.Text.RegularExpressions;
using MediatR;
using Microsoft.Extensions.Options;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Common;
using ClassroomService.Domain.Events;
using ClassroomService.Domain.DTOs;
using ClassroomService.Infrastructure.Messaging;

namespace ClassroomService.Application.Features.CrawlerChat.Commands;

public class InitiateCrawlFromChatCommandHandler
    : IRequestHandler<InitiateCrawlFromChatCommand, InitiateCrawlFromChatResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly KafkaEventPublisher _kafkaPublisher;
    private readonly KafkaSettings _kafkaSettings;
    private readonly ILogger<InitiateCrawlFromChatCommandHandler> _logger;
    private readonly IUserInfoCacheService _cacheService;
    private readonly IKafkaUserService _userService;

    public InitiateCrawlFromChatCommandHandler(
        IUnitOfWork unitOfWork,
        KafkaEventPublisher kafkaPublisher,
        IOptions<KafkaSettings> kafkaSettings,
        ILogger<InitiateCrawlFromChatCommandHandler> logger,
        IUserInfoCacheService cacheService,
        IKafkaUserService userService)
    {
        _unitOfWork = unitOfWork;
        _kafkaPublisher = kafkaPublisher;
        _kafkaSettings = kafkaSettings.Value;
        _logger = logger;
        _cacheService = cacheService;
        _userService = userService;
    }

    public async Task<InitiateCrawlFromChatResponse> Handle(
        InitiateCrawlFromChatCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. Parse message content to extract URL and prompt
            var (url, prompt) = ParseCrawlRequest(request.MessageContent);

            if (string.IsNullOrEmpty(url))
            {
                return new InitiateCrawlFromChatResponse
                {
                    Success = false,
                    Message = "Could not extract a valid URL from the message. Please include a URL to crawl."
                };
            }

            // 2. Validate MaxPages if provided
            if (request.MaxPages.HasValue && (request.MaxPages.Value < 1 || request.MaxPages.Value > 500))
            {
                _logger.LogWarning(
                    "Invalid MaxPages value {MaxPages} provided by user {UserId}",
                    request.MaxPages.Value,
                    request.SenderId);
                return new InitiateCrawlFromChatResponse
                {
                    Success = false,
                    Message = "Invalid max pages value. Please specify a number between 1 and 500."
                };
            }

            // 3. Check crawl quota before creating any chat/conversation state
            var hasQuota = await _userService.HasCrawlQuotaAsync(request.SenderId, cancellationToken);
            if (!hasQuota)
            {
                _logger.LogWarning("User {UserId} has insufficient crawl quota. Blocking conversation creation.", request.SenderId);
                return new InitiateCrawlFromChatResponse
                {
                    Success = false,
                    Message = "Crawl quota exceeded. Please upgrade your plan to continue crawling."
                };
            }

            // 3. Validate GroupId if provided
            if (request.GroupId.HasValue)
            {
                var groupExists = await _unitOfWork.Groups.GetByIdAsync(request.GroupId.Value, cancellationToken);
                if (groupExists == null)
                {
                    _logger.LogWarning(
                        "Invalid GroupId {GroupId} provided for crawl request by user {UserId}",
                        request.GroupId.Value,
                        request.SenderId);
                    return new InitiateCrawlFromChatResponse
                    {
                        Success = false,
                        Message = "Invalid group ID provided. Please select a valid group."
                    };
                }
            }

            // 4. Validate Assignment-Group relationship if both are provided
            if (request.AssignmentId.HasValue && request.GroupId.HasValue)
            {
                var assignment = await _unitOfWork.Assignments.GetAsync(
                    a => a.Id == request.AssignmentId.Value,
                    cancellationToken,
                    a => a.AssignedGroups);

                if (assignment != null && assignment.IsGroupAssignment)
                {
                    var isGroupAssigned = assignment.AssignedGroups?.Any(g => g.Id == request.GroupId.Value) ?? false;
                    if (!isGroupAssigned)
                    {
                        _logger.LogWarning(
                            "Group {GroupId} is not assigned to assignment {AssignmentId}",
                            request.GroupId.Value,
                            request.AssignmentId.Value);
                        return new InitiateCrawlFromChatResponse
                        {
                            Success = false,
                            Message = "The selected group is not assigned to this assignment."
                        };
                    }
                }
            }

            // 4. Generate unique crawl job ID
            var crawlJobId = Guid.NewGuid();

            _logger.LogInformation(
                "Initiating crawl from chat: User {UserId}, URL {Url}, Conversation {ConversationId}, JobId {JobId}",
                request.SenderId,
                url,
                request.ConversationId,
                crawlJobId);

            // 5. Create and persist chat message with CrawlJobId
            var chatMessage = new CrawlerChatMessage
            {
                Id = Guid.NewGuid(),
                ConversationId = request.ConversationId,
                SenderId = request.SenderId,
                MessageContent = request.MessageContent,
                MessageType = MessageType.CrawlRequest,
                AssignmentId = request.AssignmentId ?? Guid.Empty,
                GroupId = request.GroupId,
                CrawlJobId = crawlJobId,
                CreatedAt = DateTime.UtcNow,
                IsSystemMessage = false,
                CreatedBy = request.SenderId
            };

            await _unitOfWork.CrawlerChatMessages.AddAsync(chatMessage, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 6. Publish smart crawl request event to Kafka (fire-and-forget pattern)
            var crawlRequestEvent = new SmartCrawlRequestEvent
            {
                JobId = crawlJobId,
                ConversationId = request.ConversationId,
                AssignmentId = request.AssignmentId ?? Guid.Empty,
                GroupId = request.GroupId,
                SenderId = request.SenderId,
                SenderName = request.SenderName,
                Url = url,
                UserPrompt = string.IsNullOrEmpty(prompt)
                    ? "Extract all relevant information from the page"
                    : prompt,
                MaxPages = request.MaxPages, // Null = empty UI field (Python will handle default)
                EnableNavigationPlanning = true,
                Timestamp = DateTime.UtcNow
            };

            await _kafkaPublisher.PublishAsync(
                _kafkaSettings.SmartCrawlRequestTopic,
                crawlJobId.ToString(),
                crawlRequestEvent,
                cancellationToken);

            _logger.LogInformation(
                "Crawl request published to Kafka: MessageId {MessageId}, JobId {JobId}, Topic {Topic}",
                chatMessage.Id,
                crawlJobId,
                _kafkaSettings.SmartCrawlRequestTopic);

            return new InitiateCrawlFromChatResponse
            {
                Success = true,
                Message = "Crawl job queued successfully. You will receive real-time progress updates.",
                MessageId = chatMessage.Id,
                CrawlJobId = crawlJobId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating crawl from chat for user {UserId}", request.SenderId);
            return new InitiateCrawlFromChatResponse
            {
                Success = false,
                Message = $"Failed to initiate crawl: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Parses a natural language crawl request to extract URL and prompt
    /// Examples:
    ///   "Crawl amazon.com for laptops under $1000"
    ///   "https://example.com - extract product prices"
    ///   "Get data from https://news.com about AI trends"
    /// </summary>
    private static (string Url, string Prompt) ParseCrawlRequest(string messageContent)
    {
        // URL regex pattern (matches http://, https://, and domain.com formats)
        var urlPattern = @"(https?://[^\s]+|(?:www\.)?[a-zA-Z0-9][a-zA-Z0-9-]+\.[a-zA-Z]{2,}(?:/[^\s]*)?)";
        var urlMatch = Regex.Match(messageContent, urlPattern, RegexOptions.IgnoreCase);

        if (!urlMatch.Success)
        {
            return (string.Empty, messageContent);
        }

        var url = urlMatch.Value;

        // Ensure URL has protocol
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            url = "https://" + url;
        }

        // Extract prompt by removing the URL and common trigger words
        var prompt = messageContent
            .Replace(urlMatch.Value, string.Empty)
            .Trim();

        // Remove common crawl trigger words
        var triggerWords = new[] { "crawl", "scrape", "extract", "get data from", "analyze", "fetch", "grab", "pull data from", "-", ":" };
        foreach (var trigger in triggerWords)
        {
            prompt = Regex.Replace(prompt, $@"\b{Regex.Escape(trigger)}\b", string.Empty, RegexOptions.IgnoreCase);
        }

        prompt = prompt.Trim();

        return (url, prompt);
    }
}
