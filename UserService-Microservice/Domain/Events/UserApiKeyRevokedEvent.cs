using UserService.Domain.Common;

namespace UserService.Domain.Events;

public class UserApiKeyRevokedEvent : BaseEvent
{
    public Guid UserId { get; }
    public string UserEmail { get; }
    public Guid ApiKeyId { get; }
    public string ApiKeyName { get; }

    public UserApiKeyRevokedEvent(Guid userId, string userEmail, Guid apiKeyId, string apiKeyName)
    {
        UserId = userId;
        UserEmail = userEmail;
        ApiKeyId = apiKeyId;
        ApiKeyName = apiKeyName;
    }
}