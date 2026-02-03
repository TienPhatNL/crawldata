namespace ClassroomService.Domain.DTOs;

/// <summary>
/// User data transfer object from UserService
/// </summary>
public class UserDto
{
    /// <summary>
    /// The unique identifier of the user
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User's email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's first name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User's last name
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// User's full name
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// User's role in the system
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// User's status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Student ID (for students only)
    /// </summary>
    public string? StudentId { get; set; }

    /// <summary>
    /// Profile picture URL
    /// </summary>
    public string? ProfilePictureUrl { get; set; }

    /// <summary>
    /// When the user was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Crawl quota limit for the current plan
    /// </summary>
    public int CrawlQuotaLimit { get; set; }

    /// <summary>
    /// Crawl quota used in the current period
    /// </summary>
    public int CrawlQuotaUsed { get; set; }

    /// <summary>
    /// When the crawl quota resets
    /// </summary>
    public DateTime QuotaResetDate { get; set; }

    /// <summary>
    /// Number of unread messages from this user (populated in chat contexts)
    /// </summary>
    public int UnreadCount { get; set; } = 0;
}
