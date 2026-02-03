namespace UserService.Infrastructure.Services;

public interface ITokenService
{
    string GenerateEmailVerificationToken(Guid userId);
    string GeneratePasswordResetToken(Guid userId);
    Guid? ValidateEmailVerificationToken(string token);
    Guid? ValidatePasswordResetToken(string token);
}