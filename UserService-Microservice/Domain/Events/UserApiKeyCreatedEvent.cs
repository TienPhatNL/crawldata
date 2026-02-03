using UserService.Domain.Common;
using UserService.Domain.Enums;

namespace UserService.Domain.Events;

public class UserApiKeyCreatedEvent : BaseEvent
{
    public Guid UserId { get; }
    public Guid ApiKeyId { get; }
    public string KeyName { get; }
    public ApiKeyScope[] Scopes { get; }

    public UserApiKeyCreatedEvent(Guid userId, Guid apiKeyId, string keyName, ApiKeyScope[] scopes)
    {
        UserId = userId;
        ApiKeyId = apiKeyId;
        KeyName = keyName;
        Scopes = scopes;
    }
}