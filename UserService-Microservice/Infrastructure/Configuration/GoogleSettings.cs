namespace UserService.Infrastructure.Configuration;

public class GoogleSettings
{
    public const string SectionName = "Google";

    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
}
