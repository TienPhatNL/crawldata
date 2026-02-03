using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Mail;
using System.Net;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Infrastructure.Services;

public class EmailService : IEmailService, IDisposable
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

    public async Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        try
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(
                    _configuration["Email:FromAddress"] ?? "noreply@crawldata.com", 
                    _configuration["Email:FromName"] ?? "CrawlData Notifications"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            
            mailMessage.To.Add(toEmail);

            await _smtpClient.SendMailAsync(mailMessage, cancellationToken);
            _logger.LogInformation("Email sent successfully to {Email} with subject: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email} with subject: {Subject}", toEmail, subject);
            throw; // Rethrow so delivery service can handle retry logic
        }
    }

    public async Task SendEmailAsync(IEnumerable<string> toEmails, string subject, string body, CancellationToken cancellationToken = default)
    {
        foreach (var email in toEmails)
        {
            await SendEmailAsync(email, subject, body, cancellationToken);
        }
    }

    public void Dispose()
    {
        _smtpClient?.Dispose();
    }
}
