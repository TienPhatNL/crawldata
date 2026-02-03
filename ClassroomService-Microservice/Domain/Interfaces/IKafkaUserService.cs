using ClassroomService.Domain.DTOs;

namespace ClassroomService.Domain.Interfaces;

/// <summary>
/// Kafka-based interface for communicating with UserService
/// </summary>
public interface IKafkaUserService
{
    /// <summary>
    /// Gets a user by ID from UserService via Kafka
    /// </summary>
    Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets multiple users by their IDs from UserService via Kafka
    /// </summary>
    Task<List<UserDto>> GetUsersByIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets multiple users by their email addresses from UserService via Kafka
    /// </summary>
    Task<List<UserDto>> GetUsersByEmailsAsync(IEnumerable<string> emails, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a user exists and has the required role via Kafka
    /// </summary>
    Task<bool> ValidateUserAsync(Guid userId, string? requiredRole = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a user still has crawl quota available
    /// </summary>
    Task<bool> HasCrawlQuotaAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates student accounts in bulk via UserService using Kafka
    /// </summary>
    Task<CreateStudentAccountsResponse> CreateStudentAccountsAsync(CreateStudentAccountsRequest request, CancellationToken cancellationToken = default);
}
