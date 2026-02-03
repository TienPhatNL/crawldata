using System.Net;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.Common.Models;
using UserService.Infrastructure.Repositories;
using UserService.Infrastructure.Services;

namespace UserService.Application.Features.Authentication.Commands;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, ResponseModel>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly ILogger<ChangePasswordCommandHandler> _logger;

    public ChangePasswordCommandHandler(
        IUnitOfWork unitOfWork,
        IPasswordHashingService passwordHashingService,
        ILogger<ChangePasswordCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordHashingService = passwordHashingService;
        _logger = logger;
    }

    public async Task<ResponseModel> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null || user.IsDeleted)
        {
            _logger.LogWarning("Password change attempted for non-existent user {UserId}", request.UserId);
            throw new ValidationException("User not found");
        }

        // Check if this is a Google-authenticated user (no password)
        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            _logger.LogWarning("Password change attempted for Google-authenticated user {UserId}", user.Id);
            throw new ValidationException("This account uses Google authentication. You cannot change the password. Please continue using Google sign-in.");
        }

        // Verify current password
        if (!_passwordHashingService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            _logger.LogWarning("Password change failed: Invalid current password for user {UserId}", user.Id);
            throw new ValidationException("Current password is incorrect");
        }

        // Hash and update new password
        user.PasswordHash = _passwordHashingService.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password successfully changed for user {UserId}", user.Id);

        return new ResponseModel(HttpStatusCode.OK, "Password changed successfully");
    }
}