using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using UserService.Infrastructure.Repositories;
using UserService.Domain.Interfaces;

namespace UserService.Infrastructure.Messaging;

/// <summary>
/// Kafka consumer for UserService that handles requests from ClassroomService
/// </summary>
public class UserServiceKafkaConsumer : BackgroundService
{
    private IConsumer<string, string>? _consumer;
    private IProducer<string, string>? _producer;
    private readonly object _producerLock = new object();
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UserServiceKafkaConsumer> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _bootstrapServers;

    public UserServiceKafkaConsumer(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<UserServiceKafkaConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        // Use appsettings.json configuration (localhost:9092 for host-based services)
        // Aspire's GetConnectionString("kafka") resolves to host.docker.internal which doesn't work for host services
        _bootstrapServers = configuration["KafkaSettings:BootstrapServers"] ?? "localhost:9092";
        _logger.LogInformation("[Kafka Config] UserServiceKafkaConsumer using appsettings.json: {BootstrapServers}", _bootstrapServers);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        // Both consumer and producer will be initialized in ExecuteAsync to prevent blocking startup
        _logger.LogInformation("UserServiceKafkaConsumer created (deferred connection)");
    }

    private IProducer<string, string> GetProducer()
    {
        // Double-check locking pattern for thread-safe lazy initialization
        if (_producer != null)
            return _producer;

        lock (_producerLock)
        {
            if (_producer != null)
                return _producer;

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = _bootstrapServers,
                Acks = Acks.All,
                EnableIdempotence = true,
                ClientId = "user-service-producer"
            };

            _producer = new ProducerBuilder<string, string>(producerConfig).Build();
            _logger.LogInformation("UserService Kafka producer initialized (lazy)");
            return _producer;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Add delay to let Kafka container fully initialize
        _logger.LogInformation("UserServiceKafkaConsumer waiting 5 seconds for Kafka to be ready...");
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        try
        {
            // Initialize consumer here to avoid blocking startup
            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _bootstrapServers,
                GroupId = "user-service-consumer",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
                ClientId = "user-service-consumer",
                SessionTimeoutMs = 10000,
                SocketTimeoutMs = 10000
            };

            _logger.LogInformation("Creating UserService Kafka consumer...");
            _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
            _logger.LogInformation("✓ UserService Kafka consumer created successfully with bootstrap servers: {BootstrapServers}", _bootstrapServers);

            // Subscribe to request topics from ClassroomService
            _logger.LogInformation("Subscribing to request topics...");
            _consumer.Subscribe(new[]
            {
                "classroom.user.query.request",
                "classroom.user.validation.request",
                "classroom.student.creation.request"
            });

            _logger.LogInformation("✓ Subscribed to ClassroomService request topics");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(stoppingToken);

                    if (consumeResult != null)
                    {
                        await HandleMessage(consumeResult, stoppingToken);
                        _consumer.Commit(consumeResult);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming message: {Reason}", ex.Error.Reason);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Consumer operation cancelled");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in UserService Kafka consumer");
            throw;
        }
        finally
        {
            _consumer?.Close();
            if (_producer != null)
            {
                _producer.Flush(TimeSpan.FromSeconds(10));
            }
            _logger.LogInformation("UserService Kafka consumer closed");
        }
    }

    private async Task HandleMessage(ConsumeResult<string, string> consumeResult, CancellationToken cancellationToken)
    {
        var correlationId = consumeResult.Message.Key;
        var topic = consumeResult.Topic;

        _logger.LogDebug("Received request from topic {Topic} with correlation ID: {CorrelationId}",
            topic, correlationId);

        try
        {
            switch (topic)
            {
                case "classroom.user.query.request":
                    await HandleUserQueryRequest(consumeResult, cancellationToken);
                    break;

                case "classroom.user.validation.request":
                    await HandleUserValidationRequest(consumeResult, cancellationToken);
                    break;

                case "classroom.student.creation.request":
                    await HandleStudentCreationRequest(consumeResult, cancellationToken);
                    break;

                default:
                    _logger.LogWarning("Received message from unknown topic: {Topic}", topic);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling message from topic {Topic}", topic);
        }
    }

    private async Task HandleUserQueryRequest(ConsumeResult<string, string> message, CancellationToken cancellationToken)
    {
        try
        {
            var request = JsonSerializer.Deserialize<UserQueryRequestDto>(message.Message.Value, _jsonOptions);
            if (request == null) return;

            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var users = new List<UserDto>();
            var cacheService = scope.ServiceProvider.GetRequiredService<IUserCacheService>();

            switch (request.Type)
            {
                case QueryType.ById:
                    if (request.UserIds?.Any() == true)
                    {
                        var userId = request.UserIds.First();
                        var user = await unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
                        if (user != null)
                        {
                            var userDto = MapToUserDto(user);
                            users.Add(userDto);
                            
                            // Cache the user
                            await cacheService.SetUserAsync(userId, userDto, cancellationToken);
                            _logger.LogDebug("💾 [USER CACHED] UserId: {UserId} | Source: Kafka query response", userId);
                        }
                    }
                    break;

                case QueryType.ByIds:
                    if (request.UserIds?.Any() == true)
                    {
                        foreach (var userId in request.UserIds)
                        {
                            var user = await unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
                            if (user != null && !user.IsDeleted)
                            {
                                var userDto = MapToUserDto(user);
                                users.Add(userDto);
                                
                                // Cache the user
                                await cacheService.SetUserAsync(userId, userDto, cancellationToken);
                            }
                        }
                    }
                    break;

                case QueryType.ByEmails:
                    if (request.Emails?.Any() == true)
                    {
                        foreach (var email in request.Emails)
                        {
                            var user = await unitOfWork.Users.GetByEmailAsync(email, cancellationToken);
                            if (user != null && !user.IsDeleted)
                            {
                                var userDto = MapToUserDto(user);
                                users.Add(userDto);
                                
                                // Cache the user
                                await cacheService.SetUserAsync(user.Id, userDto, cancellationToken);
                            }
                        }
                    }
                    break;
            }

            var response = new UserQueryResponseDto
            {
                CorrelationId = request.CorrelationId,
                Success = true,
                Users = users,
                RespondedAt = DateTime.UtcNow
            };

            await PublishResponse("classroom.user.query.response", request.CorrelationId, response, cancellationToken);
            _logger.LogInformation("Sent user query response for correlation ID: {CorrelationId}, users count: {Count}",
                request.CorrelationId, users.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling user query request");
            await PublishErrorResponse("classroom.user.query.response", message.Message.Key, ex.Message, cancellationToken);
        }
    }

    private async Task HandleUserValidationRequest(ConsumeResult<string, string> message, CancellationToken cancellationToken)
    {
        try
        {
            var request = JsonSerializer.Deserialize<UserValidationRequestDto>(message.Message.Value, _jsonOptions);
            if (request == null) return;

            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var user = await unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
            bool isValid = user != null && !user.IsDeleted;

            if (isValid && !string.IsNullOrEmpty(request.RequiredRole))
            {
                isValid = user!.Role.ToString().Equals(request.RequiredRole, StringComparison.OrdinalIgnoreCase);
            }

            var response = new UserValidationResponseDto
            {
                CorrelationId = request.CorrelationId,
                IsValid = isValid,
                RespondedAt = DateTime.UtcNow
            };

            await PublishResponse("classroom.user.validation.response", request.CorrelationId, response, cancellationToken);
            _logger.LogInformation("Sent user validation response for user {UserId}: {IsValid}",
                request.UserId, isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling user validation request");
            await PublishErrorResponse("classroom.user.validation.response", message.Message.Key, ex.Message, cancellationToken);
        }
    }

    private async Task HandleStudentCreationRequest(ConsumeResult<string, string> message, CancellationToken cancellationToken)
    {
        try
        {
            var request = JsonSerializer.Deserialize<StudentCreationRequestDto>(message.Message.Value, _jsonOptions);
            if (request == null || request.Request == null) return;

            _logger.LogInformation("Processing student creation request for {Count} students", 
                request.Request.Students?.Count ?? 0);

            using var scope = _serviceProvider.CreateScope();
            var studentAccountService = scope.ServiceProvider.GetRequiredService<IStudentAccountService>();

            // Map Kafka request to Domain service request
            var serviceRequest = new CreateStudentAccountsRequest
            {
                RequestedBy = request.Request.RequestedBy,
                SendEmailCredentials = request.Request.SendEmailCredentials,
                CreateAccountIfNotFound = request.Request.CreateAccountIfNotFound,
                Students = request.Request.Students?.Select(s => new StudentAccountInfo
                {
                    Email = s.Email,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    StudentId = s.StudentId,
                    PhoneNumber = s.PhoneNumber
                }).ToList() ?? new List<StudentAccountInfo>()
            };

            // Execute the service method
            var result = await studentAccountService.CreateStudentAccountsAsync(serviceRequest, cancellationToken);

            // Map result to Kafka response
            var response = new StudentCreationResponseDto
            {
                CorrelationId = request.CorrelationId,
                Response = new CreateStudentAccountsResponseDto
                {
                    Success = result.IsSuccess,
                    Message = result.Message,
                    TotalRequested = result.TotalRequested,
                    SuccessfullyCreated = result.SuccessfullyCreated,
                    Failed = result.Failed,
                    Results = result.Results.Select(r => new StudentCreationResultDto
                    {
                        Email = r.Email,
                        StudentId = r.StudentId,
                        Success = r.Success,
                        ErrorMessage = r.ErrorMessage,
                        UserId = r.UserId,
                        TemporaryPassword = r.TemporaryPassword
                    }).ToList()
                },
                RespondedAt = DateTime.UtcNow
            };

            await PublishResponse("classroom.student.creation.response", request.CorrelationId, response, cancellationToken);
            _logger.LogInformation("Sent student creation response for correlation ID: {CorrelationId}, created: {Created}/{Total}",
                request.CorrelationId, response.Response.SuccessfullyCreated, response.Response.TotalRequested);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling student creation request");
            await PublishErrorResponse("classroom.student.creation.response", message.Message.Key, ex.Message, cancellationToken);
        }
    }

    private async Task PublishResponse<T>(string topic, string correlationId, T response, CancellationToken cancellationToken)
    {
        var messageContent = JsonSerializer.Serialize(response, _jsonOptions);
        var message = new Message<string, string>
        {
            Key = correlationId,
            Value = messageContent,
            Headers = new Headers
            {
                { "timestamp", System.Text.Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")) },
                { "service", System.Text.Encoding.UTF8.GetBytes("UserService") }
            }
        };

        var producer = GetProducer();
        await producer.ProduceAsync(topic, message, cancellationToken);
    }

    private async Task PublishErrorResponse(string topic, string correlationId, string errorMessage, CancellationToken cancellationToken)
    {
        var response = new
        {
            correlationId,
            success = false,
            errorMessage,
            respondedAt = DateTime.UtcNow
        };

        await PublishResponse(topic, correlationId, response, cancellationToken);
    }

    private UserDto MapToUserDto(UserService.Domain.Entities.User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            Status = user.Status.ToString(),
            StudentId = user.StudentId,
            ProfilePictureUrl = user.ProfilePictureUrl,
            CreatedAt = user.CreatedAt,
            CrawlQuotaLimit = user.CrawlQuotaLimit,
            CrawlQuotaUsed = user.CrawlQuotaUsed,
            QuotaResetDate = user.QuotaResetDate
        };
    }

    public override void Dispose()
    {
        _consumer?.Dispose();
        _producer?.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}

// DTOs for message contracts
public class UserQueryRequestDto
{
    public string CorrelationId { get; set; } = string.Empty;
    public QueryType Type { get; set; }
    public List<Guid>? UserIds { get; set; }
    public List<string>? Emails { get; set; }
}

public class UserQueryResponseDto
{
    public string CorrelationId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public List<UserDto> Users { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public DateTime RespondedAt { get; set; }
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? StudentId { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CrawlQuotaLimit { get; set; }
    public int CrawlQuotaUsed { get; set; }
    public DateTime QuotaResetDate { get; set; }
}

public class UserValidationRequestDto
{
    public string CorrelationId { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string? RequiredRole { get; set; }
}

public class UserValidationResponseDto
{
    public string CorrelationId { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime RespondedAt { get; set; }
}

public class StudentCreationRequestDto
{
    public string CorrelationId { get; set; } = string.Empty;
    public CreateStudentAccountsRequestDto? Request { get; set; }
}

public class CreateStudentAccountsRequestDto
{
    public Guid RequestedBy { get; set; }
    public List<StudentInfoDto>? Students { get; set; }
    public bool SendEmailCredentials { get; set; } = true;
    public bool CreateAccountIfNotFound { get; set; } = true;
}

public class StudentInfoDto
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}

public class StudentCreationResponseDto
{
    public string CorrelationId { get; set; } = string.Empty;
    public CreateStudentAccountsResponseDto Response { get; set; } = new();
    public DateTime RespondedAt { get; set; }
}

public class CreateStudentAccountsResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TotalRequested { get; set; }
    public int SuccessfullyCreated { get; set; }
    public int Failed { get; set; }
    public List<StudentCreationResultDto> Results { get; set; } = new();
}

public class StudentCreationResultDto
{
    public string Email { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? UserId { get; set; }
    public string? TemporaryPassword { get; set; }
}

/// <summary>
/// Type of user query - matches ClassroomService.Domain.Messages.QueryType
/// </summary>
public enum QueryType
{
    ById = 0,
    ByIds = 1,
    ByEmails = 2
}
