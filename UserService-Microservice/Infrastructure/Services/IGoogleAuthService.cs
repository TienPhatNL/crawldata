using Google.Apis.Auth;

namespace UserService.Infrastructure.Services;

public interface IGoogleAuthService
{
    Task<GoogleJsonWebSignature.Payload> ValidateGoogleTokenAsync(string idToken, CancellationToken cancellationToken = default);
}
