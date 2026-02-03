using System.Security.Claims;

namespace WebCrawlerService.Domain.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(IEnumerable<Claim> claims);
    string GenerateRefreshToken();
    ClaimsPrincipal GetClaimsPrincipalFromToken(string token);
    bool ValidateToken(string token);
    DateTime GetTokenExpiration(string token);
}