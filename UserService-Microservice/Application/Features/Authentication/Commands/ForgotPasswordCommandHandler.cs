using System.Net;
using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.Common.Interfaces;
using UserService.Application.Common.Models;
using UserService.Domain.Events;
using UserService.Infrastructure.Repositories;
using UserService.Infrastructure.Services;
using UserService.Infrastructure.Messaging;

namespace UserService.Application.Features.Authentication.Commands;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, ResponseModel>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ITokenService tokenService,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<ResponseModel> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        // Always return success message for security (don't reveal if email exists)
        var message = "If the email exists, you will receive a password reset link.";

        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);

        if (user == null || user.IsDeleted)
        {
            _logger.LogWarning("Password reset requested for non-existent email: {Email}", request.Email);
            return new ResponseModel(HttpStatusCode.OK, message); // Don't reveal that email doesn't exist
        }

        // Generate password reset token (valid for 1 hour)
        var resetToken = _tokenService.GeneratePasswordResetToken(user.Id);
        user.PasswordResetToken = resetToken;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);

        // Add domain event (will be auto-dispatched by SaveChangesAsync)
        user.AddDomainEvent(new PasswordResetRequestedEvent(
            user.Id,
            user.Email,
            resetToken,
            DateTime.UtcNow,
            null, // IP address not captured in current command
            null)); // Location not captured in current command

        await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send password reset email
        await _emailService.SendPasswordResetEmailAsync(
            user.Email,
            $"{user.FirstName} {user.LastName}",
            resetToken,
            cancellationToken);

        _logger.LogInformation("Password reset email sent to user {UserId}", user.Id);

        return new ResponseModel(HttpStatusCode.OK, message);
    }
}