using System.Net;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.Common.Models;
using UserService.Infrastructure.Repositories;
using UserService.Infrastructure.Services;

namespace UserService.Application.Features.Authentication.Commands;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ResponseModel>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        IUnitOfWork unitOfWork,
        IPasswordHashingService passwordHashingService,
        ITokenService tokenService,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordHashingService = passwordHashingService;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<ResponseModel> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        // Validate and decode reset token
        var userId = _tokenService.ValidatePasswordResetToken(request.Token);
        if (userId == null)
        {
            _logger.LogWarning("Invalid or expired password reset token provided");
            throw new ValidationException("Invalid or expired reset token");
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId.Value, cancellationToken);
        if (user == null || user.IsDeleted)
        {
            _logger.LogWarning("Password reset attempted for non-existent user {UserId}", userId);
            throw new ValidationException("Invalid or expired reset token");
        }

        _logger.LogDebug("Password reset token validation successful for user {UserId}", user.Id);

        // Hash new password
        user.PasswordHash = _passwordHashingService.HashPassword(request.NewPassword);

        // Clear reset token
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;

        // Reset failed login attempts
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;

        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Users.UpdateAsync(user, cancellationToken);

        // Invalidate all existing sessions for security
        var userSessions = await _unitOfWork.UserSessions.GetManyAsync(s => s.UserId == user.Id && s.IsActive, cancellationToken);
        foreach (var session in userSessions)
        {
            session.IsActive = false;
            session.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.UserSessions.UpdateAsync(session, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password successfully reset for user {UserId}", user.Id);

        return new ResponseModel(HttpStatusCode.OK, "Password reset successful. Please login with your new password.");
    }
}