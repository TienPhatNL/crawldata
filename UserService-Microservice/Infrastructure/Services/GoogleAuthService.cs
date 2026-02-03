using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UserService.Infrastructure.Configuration;

namespace UserService.Infrastructure.Services;

public class GoogleAuthService : IGoogleAuthService
{
    private readonly GoogleSettings _googleSettings;
    private readonly ILogger<GoogleAuthService> _logger;

    public GoogleAuthService(
        IOptions<GoogleSettings> googleSettings,
        ILogger<GoogleAuthService> logger)
    {
        _googleSettings = googleSettings.Value;
        _logger = logger;
    }

    public async Task<GoogleJsonWebSignature.Payload> ValidateGoogleTokenAsync(
        string idToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            _logger.LogWarning("Google token validation attempted with null or empty token");
            throw new ArgumentException("ID token cannot be null or empty", nameof(idToken));
        }

        try
        {
            _logger.LogDebug("Validating Google ID token");

            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _googleSettings.ClientId }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            if (payload == null)
            {
                _logger.LogWarning("Google token validation returned null payload");
                throw new UnauthorizedAccessException("Invalid Google token");
            }

            _logger.LogInformation("Google token validated successfully for user: {Email}", payload.Email);
            return payload;
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning(ex, "Invalid Google JWT token");
            throw new UnauthorizedAccessException("Invalid Google token", ex);
        }
        catch (Exception ex) when (ex is not UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Unexpected error during Google token validation");
            throw new InvalidOperationException("Google authentication service error", ex);
        }
    }
}
