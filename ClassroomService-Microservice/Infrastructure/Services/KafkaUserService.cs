using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Common;
using ClassroomService.Domain.Messages;
using ClassroomService.Infrastructure.Messaging;

namespace ClassroomService.Infrastructure.Services;

/// <summary>
/// Kafka-based implementation for communicating with UserService with Redis caching and stampede prevention
/// </summary>
public class KafkaUserService : IKafkaUserService
{
    private readonly KafkaEventPublisher _publisher;
    private readonly MessageCorrelationManager _correlationManager;
    private readonly KafkaSettings _kafkaSettings;
    private readonly IUserInfoCacheService _cacheService;
    private readonly ILogger<KafkaUserService> _logger;
    private readonly TimeSpan _requestTimeout;

    public KafkaUserService(
        KafkaEventPublisher publisher,
        MessageCorrelationManager correlationManager,
        IOptions<KafkaSettings> kafkaSettings,
        IUserInfoCacheService cacheService,
        ILogger<KafkaUserService> logger)
    {
        _publisher = publisher;
        _correlationManager = correlationManager;
        _kafkaSettings = kafkaSettings.Value;
        _cacheService = cacheService;
        _logger = logger;
        _requestTimeout = TimeSpan.FromSeconds(_kafkaSettings.RequestTimeoutSeconds);
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Use cache with stampede prevention
        return await _cacheService.GetOrFetchUserAsync(
            userId,
            async () => await FetchUserFromKafkaAsync(userId, cancellationToken),
            cancellationToken
        );
    }

    private async Task<UserDto?> FetchUserFromKafkaAsync(Guid userId, CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid().ToString();
        
        var request = new UserQueryRequest
        {
            CorrelationId = correlationId,
            Type = QueryType.ById,
            UserIds = new List<Guid> { userId }
        };

        try
        {
            _logger.LogDebug("Fetching user from Kafka for user ID: {UserId}, correlation ID: {CorrelationId}", 
                userId, correlationId);

            // Register pending request
            var responseTask = _correlationManager.RegisterRequest<UserQueryResponse>(
                correlationId,
                _requestTimeout
            );

            // Publish request
            await _publisher.PublishAsync(
                _kafkaSettings.UserQueryRequestTopic,
                correlationId,
                request,
                cancellationToken
            );

            // Wait for response
            var response = await responseTask;

            if (response.Success)
            {
                _logger.LogInformation("Successfully received user data from Kafka for user ID: {UserId}", userId);
                return response.Users.FirstOrDefault();
            }
            else
            {
                _logger.LogWarning("Failed to get user {UserId} from Kafka: {ErrorMessage}", userId, response.ErrorMessage);
                return null;
            }
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout waiting for user query response for user ID: {UserId}", userId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying user by ID from Kafka: {UserId}", userId);
            return null;
        }
    }

    public async Task<List<UserDto>> GetUsersByIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
    {
        var userIdsList = userIds.ToList();
        if (!userIdsList.Any())
            return new List<UserDto>();

        // Try to get cached users first
        var cachedUsers = await _cacheService.GetUsersByIdsAsync(userIdsList, cancellationToken);
        var uncachedUserIds = userIdsList.Where(id => cachedUsers[id] == null).ToList();

        if (!uncachedUserIds.Any())
        {
            // All users are cached
            _logger.LogDebug("All {Count} users found in cache", userIdsList.Count);
            return cachedUsers.Values.Where(u => u != null).ToList()!;
        }

        _logger.LogDebug("Cache: {Cached} hits, {Uncached} misses out of {Total} users", 
            userIdsList.Count - uncachedUserIds.Count, uncachedUserIds.Count, userIdsList.Count);

        // Fetch uncached users from Kafka
        var fetchedUsers = await FetchUsersFromKafkaAsync(uncachedUserIds, cancellationToken);

        // Cache the fetched users
        if (fetchedUsers.Any())
        {
            var usersToCache = fetchedUsers.ToDictionary(u => u.Id, u => u);
            await _cacheService.SetUsersAsync(usersToCache, cancellationToken);
        }

        // Combine cached and fetched users
        var allUsers = cachedUsers.Values.Where(u => u != null).Concat(fetchedUsers).ToList();
        return allUsers!;
    }

    private async Task<List<UserDto>> FetchUsersFromKafkaAsync(List<Guid> userIds, CancellationToken cancellationToken)
    {
        if (!userIds.Any())
            return new List<UserDto>();

        var correlationId = Guid.NewGuid().ToString();
        
        var request = new UserQueryRequest
        {
            CorrelationId = correlationId,
            Type = QueryType.ByIds,
            UserIds = userIds
        };

        try
        {
            _logger.LogDebug("Fetching {Count} users from Kafka, correlation ID: {CorrelationId}", 
                userIds.Count, correlationId);

            // Register pending request
            var responseTask = _correlationManager.RegisterRequest<UserQueryResponse>(
                correlationId,
                _requestTimeout
            );

            // Publish request
            await _publisher.PublishAsync(
                _kafkaSettings.UserQueryRequestTopic,
                correlationId,
                request,
                cancellationToken
            );

            // Wait for response
            var response = await responseTask;

            if (response.Success)
            {
                _logger.LogInformation("Successfully received {Count} users from Kafka", response.Users.Count);
                return response.Users;
            }
            else
            {
                _logger.LogWarning("Failed to get users from Kafka: {ErrorMessage}", response.ErrorMessage);
                return new List<UserDto>();
            }
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout waiting for batch user query response");
            return new List<UserDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying users by IDs");
            return new List<UserDto>();
        }
    }

    public async Task<List<UserDto>> GetUsersByEmailsAsync(IEnumerable<string> emails, CancellationToken cancellationToken = default)
    {
        var emailsList = emails.ToList();
        if (!emailsList.Any())
            return new List<UserDto>();

        var correlationId = Guid.NewGuid().ToString();
        
        var request = new UserQueryRequest
        {
            CorrelationId = correlationId,
            Type = QueryType.ByEmails,
            Emails = emailsList
        };

        try
        {
            _logger.LogDebug("Sending user query request by emails for {Count} emails, correlation ID: {CorrelationId}", 
                emailsList.Count, correlationId);

            // Register pending request
            var responseTask = _correlationManager.RegisterRequest<UserQueryResponse>(
                correlationId,
                _requestTimeout
            );

            // Publish request
            await _publisher.PublishAsync(
                _kafkaSettings.UserQueryRequestTopic,
                correlationId,
                request,
                cancellationToken
            );

            // Wait for response
            var response = await responseTask;

            if (response.Success)
            {
                _logger.LogInformation("Successfully received {Count} users by email", response.Users.Count);
                return response.Users;
            }
            else
            {
                _logger.LogWarning("Failed to get users by email: {ErrorMessage}", response.ErrorMessage);
                return new List<UserDto>();
            }
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout waiting for user query by email response");
            return new List<UserDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying users by emails");
            return new List<UserDto>();
        }
    }

    public async Task<bool> ValidateUserAsync(Guid userId, string? requiredRole = null, CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString();
        
        var request = new UserValidationRequest
        {
            CorrelationId = correlationId,
            UserId = userId,
            RequiredRole = requiredRole
        };

        try
        {
            _logger.LogDebug("Sending user validation request for user ID: {UserId}, role: {Role}, correlation ID: {CorrelationId}", 
                userId, requiredRole ?? "any", correlationId);

            // Register pending request
            var responseTask = _correlationManager.RegisterRequest<UserValidationResponse>(
                correlationId,
                _requestTimeout
            );

            // Publish request
            await _publisher.PublishAsync(
                _kafkaSettings.UserValidationRequestTopic,
                correlationId,
                request,
                cancellationToken
            );

            // Wait for response
            var response = await responseTask;

            _logger.LogInformation("User validation result for {UserId}: {IsValid}", userId, response.IsValid);
            return response.IsValid;
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout waiting for user validation response for user ID: {UserId}", userId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating user: {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> HasCrawlQuotaAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await FetchUserFromKafkaAsync(userId, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("Unable to check crawl quota: user {UserId} not found (allowing request)", userId);
            return true;
        }

        await _cacheService.SetUserAsync(userId, user, cancellationToken);

        var quotaLimit = Math.Max(0, user.CrawlQuotaLimit);
        var quotaUsed = Math.Max(0, user.CrawlQuotaUsed);
        if (user.QuotaResetDate != default && user.QuotaResetDate <= DateTime.UtcNow)
        {
            quotaUsed = 0;
        }

        var remaining = quotaLimit - quotaUsed;

        _logger.LogDebug("User {UserId} crawl quota: {Used}/{Limit} (remaining {Remaining})",
            userId, quotaUsed, quotaLimit, remaining);

        return remaining > 0;
    }

    public async Task<CreateStudentAccountsResponse> CreateStudentAccountsAsync(
        CreateStudentAccountsRequest request, 
        CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString();
        
        var kafkaRequest = new StudentCreationRequest
        {
            CorrelationId = correlationId,
            Request = request
        };

        try
        {
            _logger.LogDebug("Sending student creation request for {Count} students, correlation ID: {CorrelationId}", 
                request.Students.Count, correlationId);

            // Register pending request
            var responseTask = _correlationManager.RegisterRequest<StudentCreationResponse>(
                correlationId,
                TimeSpan.FromSeconds(60) // Longer timeout for bulk operation
            );

            // Publish request
            await _publisher.PublishAsync(
                _kafkaSettings.StudentCreationRequestTopic,
                correlationId,
                kafkaRequest,
                cancellationToken
            );

            // Wait for response
            var response = await responseTask;

            _logger.LogInformation("Student creation completed: {Success}/{Total} students created", 
                response.Response.SuccessfullyCreated, response.Response.TotalRequested);
            
            return response.Response;
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout waiting for student creation response");
            return new CreateStudentAccountsResponse
            {
                Success = false,
                Message = "Request timed out",
                TotalRequested = request.Students.Count,
                SuccessfullyCreated = 0,
                Failed = request.Students.Count,
                Results = request.Students.Select(s => new StudentCreationResult
                {
                    Email = s.Email,
                    StudentId = s.StudentId,
                    Success = false,
                    ErrorMessage = "Request timed out"
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating student accounts");
            return new CreateStudentAccountsResponse
            {
                Success = false,
                Message = $"Error: {ex.Message}",
                TotalRequested = request.Students.Count,
                SuccessfullyCreated = 0,
                Failed = request.Students.Count,
                Results = request.Students.Select(s => new StudentCreationResult
                {
                    Email = s.Email,
                    StudentId = s.StudentId,
                    Success = false,
                    ErrorMessage = ex.Message
                }).ToList()
            };
        }
    }
}
