namespace ClassroomService.Infrastructure.Common;

public class JwtSettings
{
    public const string SectionName = "Jwt";
    
    public string SecretKey { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; init; } = 120;
    public int RefreshTokenExpirationDays { get; init; } = 7;
    
    // For compatibility with JWT services that expect ExpiryMinutes
    public int ExpiryMinutes => AccessTokenExpirationMinutes;
}