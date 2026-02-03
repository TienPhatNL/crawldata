namespace ClassroomService.Domain.Messages;

/// <summary>
/// Request message for querying users from UserService
/// </summary>
public class UserQueryRequest
{
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    public QueryType Type { get; set; }
    public List<Guid>? UserIds { get; set; }
    public List<string>? Emails { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Type of user query
/// </summary>
public enum QueryType
{
    ById,
    ByIds,
    ByEmails
}
