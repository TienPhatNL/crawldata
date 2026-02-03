using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Mail;
using System.Net;
using System.Text;

namespace UserService.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly SmtpClient _smtpClient;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        // Configure SMTP client
        _smtpClient = new SmtpClient
        {
            Host = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com",
            Port = int.Parse(_configuration["Email:SmtpPort"] ?? "587"),
            EnableSsl = bool.Parse(_configuration["Email:EnableSsl"] ?? "true"),
            Credentials = new NetworkCredential(
                _configuration["Email:Username"], 
                _configuration["Email:Password"])
        };
    }

    public async Task SendEmailVerificationAsync(string email, string fullName, string verificationToken, CancellationToken cancellationToken = default)
    {
        var subject = "Verify Your Email Address - CrawlData Platform";
        var verificationLink = $"{_configuration["App:BaseUrl"]}/verify-email?token={verificationToken}";
        
        var body = $@"
            <html>
            <body>
                <h2>Email Verification Required</h2>
                <p>Hello {fullName},</p>
                <p>Thank you for registering with CrawlData Platform. Please verify your email address by clicking the link below:</p>
                <p><a href='{verificationLink}' style='background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; border-radius: 4px;'>Verify Email Address</a></p>
                <p>If you cannot click the link, copy and paste this URL into your browser:</p>
                <p>{verificationLink}</p>
                <p>This link will expire in 24 hours.</p>
                <br>
                <p>Best regards,<br>CrawlData Platform Team</p>
            </body>
            </html>";

        await SendEmailAsync(email, subject, body, cancellationToken);
    }

    public async Task SendPasswordResetEmailAsync(string email, string fullName, string resetToken, CancellationToken cancellationToken = default)
    {
        var subject = "Password Reset Request - CrawlData Platform";
        var resetLink = $"{_configuration["App:BaseUrl"]}/reset-password?token={resetToken}";
        
        var body = $@"
            <html>
            <body>
                <h2>Password Reset Request</h2>
                <p>Hello {fullName},</p>
                <p>We received a request to reset your password. Click the link below to create a new password:</p>
                <p><a href='{resetLink}' style='background-color: #FF6B6B; color: white; padding: 10px 20px; text-decoration: none; border-radius: 4px;'>Reset Password</a></p>
                <p>If you cannot click the link, copy and paste this URL into your browser:</p>
                <p>{resetLink}</p>
                <p>This link will expire in 1 hour. If you didn't request a password reset, you can safely ignore this email.</p>
                <br>
                <p>Best regards,<br>CrawlData Platform Team</p>
            </body>
            </html>";

        await SendEmailAsync(email, subject, body, cancellationToken);
    }

    public async Task SendUserApprovedEmailAsync(string email, string fullName, CancellationToken cancellationToken = default)
    {
        var subject = "Account Approved - CrawlData Platform";
        var loginLink = $"{_configuration["App:BaseUrl"]}/login";
        
        var body = $@"
            <html>
            <body>
                <h2>Account Approved!</h2>
                <p>Hello {fullName},</p>
                <p>Great news! Your CrawlData Platform account has been approved by our staff.</p>
                <p>You can now access all features available to your account type.</p>
                <p><a href='{loginLink}' style='background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; border-radius: 4px;'>Login to Your Account</a></p>
                <p>Thank you for joining the CrawlData Platform community!</p>
                <br>
                <p>Best regards,<br>CrawlData Platform Team</p>
            </body>
            </html>";

        await SendEmailAsync(email, subject, body, cancellationToken);
    }

    public async Task SendWelcomeEmailAsync(string email, string fullName, CancellationToken cancellationToken = default)
    {
        var subject = "Welcome to CrawlData Platform!";
        var loginLink = $"{_configuration["App:BaseUrl"]}/login";
        
        var body = $@"
            <html>
            <body>
                <h2>Welcome to CrawlData Platform!</h2>
                <p>Hello {fullName},</p>
                <p>Welcome to CrawlData Platform! Your account has been successfully created and verified.</p>
                <p>You can now start using our web crawling and data extraction services.</p>
                <p><a href='{loginLink}' style='background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; border-radius: 4px;'>Login to Your Account</a></p>
                <p>If you have any questions, please don't hesitate to contact our support team.</p>
                <br>
                <p>Best regards,<br>CrawlData Platform Team</p>
            </body>
            </html>";

        await SendEmailAsync(email, subject, body, cancellationToken);
    }

    public async Task SendAccountSuspendedEmailAsync(string email, string fullName, string reason, CancellationToken cancellationToken = default)
    {
        var subject = "Account Suspended - CrawlData Platform";
        var contactLink = $"{_configuration["App:BaseUrl"]}/contact";
        
        var body = $@"
            <html>
            <body>
                <h2>Account Suspended</h2>
                <p>Hello {fullName},</p>
                <p>We regret to inform you that your CrawlData Platform account has been temporarily suspended.</p>
                <p><strong>Reason:</strong> {reason}</p>
                <p>If you believe this is an error or would like to appeal this decision, please contact our support team:</p>
                <p><a href='{contactLink}' style='background-color: #FF6B6B; color: white; padding: 10px 20px; text-decoration: none; border-radius: 4px;'>Contact Support</a></p>
                <br>
                <p>Best regards,<br>CrawlData Platform Team</p>
            </body>
            </html>";

        await SendEmailAsync(email, subject, body, cancellationToken);
    }

    public async Task SendStudentAccountCreatedEmailAsync(string email, string firstName, string studentId, string temporaryPassword, CancellationToken cancellationToken = default)
    {
        var subject = "Your Student Account Has Been Created - CrawlData Platform";
        var loginLink = $"{_configuration["App:BaseUrl"]}/login";
        
        var body = $@"
            <html>
            <body>
                <h2>Welcome {firstName}!</h2>
                <p>A student account has been created for you on the CrawlData Platform by your lecturer or staff member.</p>
                <p><strong>Student ID:</strong> {studentId}</p>
                <p><strong>Email:</strong> {email}</p>
                <p><strong>Temporary Password:</strong> <code style='background-color: #f4f4f4; padding: 2px 6px; border-radius: 3px;'>{temporaryPassword}</code></p>
                <br>
                <p><strong>Important Security Notice:</strong></p>
                <ul>
                    <li>Please login and change your password immediately</li>
                    <li>Your account has been automatically verified - no email confirmation needed</li>
                    <li>Do not share your password with anyone</li>
                    <li>This is a one-time credential notification</li>
                </ul>
                <p><a href='{loginLink}' style='background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; border-radius: 4px;'>Login Now</a></p>
                <br>
                <p>If you have any questions, please contact your course instructor or our support team.</p>
                <br>
                <p>Best regards,<br>CrawlData Platform Team</p>
            </body>
            </html>";

        await SendEmailAsync(email, subject, body, cancellationToken);
    }

    private async Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        try
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_configuration["Email:FromAddress"] ?? "noreply@crawldata.com", "CrawlData Platform"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            
            mailMessage.To.Add(to);

            await _smtpClient.SendMailAsync(mailMessage, cancellationToken);
            _logger.LogInformation("Email sent successfully to {Email} with subject: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email} with subject: {Subject}", to, subject);
            // In a production environment, you might want to throw or handle this differently
            // For now, we'll just log the error and continue
        }
    }

    public void Dispose()
    {
        _smtpClient?.Dispose();
    }
}