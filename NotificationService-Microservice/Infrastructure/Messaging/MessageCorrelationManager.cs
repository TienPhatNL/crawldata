namespace NotificationService.Infrastructure.Messaging;

public class MessageCorrelationManager
{
    private static readonly AsyncLocal<string?> _correlationId = new();

    public string GetOrCreateCorrelationId()
    {
        if (string.IsNullOrEmpty(_correlationId.Value))
        {
            _correlationId.Value = Guid.NewGuid().ToString();
        }

        return _correlationId.Value;
    }

    public void SetCorrelationId(string correlationId)
    {
        _correlationId.Value = correlationId;
    }

    public string? GetCorrelationId()
    {
        return _correlationId.Value;
    }

    public void ClearCorrelationId()
    {
        _correlationId.Value = null;
    }
}
