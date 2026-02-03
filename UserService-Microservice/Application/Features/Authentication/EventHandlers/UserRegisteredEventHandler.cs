using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Domain.Events;
using UserService.Infrastructure.Services;
using UserService.Infrastructure.Repositories;
using UserService.Infrastructure.Messaging;

namespace UserService.Application.Features.Authentication.EventHandlers;

public class UserRegisteredEventHandler : INotificationHandler<UserRegisteredEvent>
{
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserRegisteredEventHandler> _logger;
    private readonly KafkaEventPublisher _eventPublisher;

    public UserRegisteredEventHandler(
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        ILogger<UserRegisteredEventHandler> logger,
        KafkaEventPublisher eventPublisher)
    {
        _emailService = emailService;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            if (notification.RequiresApproval)
            {
                _logger.LogInformation("User {UserId} with email {Email} registered but requires approval", 
                    notification.UserId, notification.Email);
                // For users requiring approval, we'll send the email after approval
                return;
            }

            // For users that don't require approval, send verification email
            _logger.LogInformation("Sending email verification to user {UserId} with email {Email}", 
                notification.UserId, notification.Email);

            // Get user details for email sending
            var user = await _unitOfWork.Users.GetByIdAsync(notification.UserId, cancellationToken);
            if (user != null && !string.IsNullOrEmpty(user.EmailVerificationToken))
            {
                await _emailService.SendEmailVerificationAsync(
                    notification.Email,
                    $"{user.FirstName} {user.LastName}",
                    user.EmailVerificationToken,
                    cancellationToken);

                _logger.LogInformation("Email verification sent to {Email} for user {UserId}", 
                    notification.Email, notification.UserId);
            }
            else
            {
                _logger.LogWarning("Unable to send verification email - user or token not found for {UserId}", 
                    notification.UserId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email to user {UserId} with email {Email}", 
                notification.UserId, notification.Email);
            // Don't throw - email sending failure shouldn't fail the registration process
        }

        // Publish to Kafka for NotificationService (always publish, regardless of email result)
        try
        {
            await _eventPublisher.PublishEventAsync(
                "user-events",
                notification,
                cancellationToken);
            
            _logger.LogDebug("UserRegisteredEvent published to Kafka for user {UserId}", notification.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish UserRegisteredEvent to Kafka for user {UserId}", notification.UserId);
            throw;
        }
    }
}