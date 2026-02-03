using System.Security.Claims;

namespace UserService.Infrastructure.Services;

public interface IJwtTokenService
{
    string GenerateAccessToken(IEnumerable<Claim> claims);
    string GenerateRefreshToken();
    ClaimsPrincipal GetClaimsPrincipalFromToken(string token);
    bool ValidateToken(string token);
    DateTime GetTokenExpiration(string token);
    IEnumerable<Claim> GetUserClaims(Guid userId, string email, string role, string subscriptionTier);
}