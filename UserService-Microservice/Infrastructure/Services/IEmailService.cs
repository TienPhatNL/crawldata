namespace UserService.Infrastructure.Services;

public interface IEmailService
{
    Task SendEmailVerificationAsync(string email, string fullName, string verificationToken, CancellationToken cancellationToken = default);
    Task SendPasswordResetEmailAsync(string email, string fullName, string resetToken, CancellationToken cancellationToken = default);
    Task SendUserApprovedEmailAsync(string email, string fullName, CancellationToken cancellationToken = default);
    Task SendWelcomeEmailAsync(string email, string fullName, CancellationToken cancellationToken = default);
    Task SendAccountSuspendedEmailAsync(string email, string fullName, string reason, CancellationToken cancellationToken = default);
    Task SendStudentAccountCreatedEmailAsync(string email, string firstName, string studentId, string temporaryPassword, CancellationToken cancellationToken = default);
}