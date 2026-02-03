using System.Net;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.Common.Models;
using UserService.Domain.Enums;
using UserService.Domain.Events;
using UserService.Infrastructure.Repositories;

namespace UserService.Application.Features.Authentication.Commands;

public class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand, ResponseModel>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ConfirmEmailCommandHandler> _logger;

    public ConfirmEmailCommandHandler(IUnitOfWork unitOfWork, ILogger<ConfirmEmailCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ResponseModel> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        // Find user by verification token
        var user = await _unitOfWork.Users.GetByEmailVerificationTokenAsync(request.Token, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Email confirmation failed: Invalid or expired token {Token}", request.Token);
            throw new ValidationException("Invalid or expired verification token");
        }

        return await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            // Confirm email
            user.EmailConfirmedAt = DateTime.UtcNow;
            user.EmailVerificationToken = null;
            user.EmailVerificationTokenExpires = null;

            // Update status to Active if currently Pending (email confirmation completes the registration)
            // Users with PendingApproval status require staff approval and should remain in that state
            if (user.Status == UserStatus.Pending)
            {
                user.Status = UserStatus.Active;
                _logger.LogInformation("User {UserId} status updated from Pending to Active after email confirmation", user.Id);
            }

            // Add domain event
            user.AddDomainEvent(new UserEmailConfirmedEvent(user.Id, user.Email));

            await _unitOfWork.Users.UpdateAsync(user, cancellationToken);

            _logger.LogInformation("Email confirmed for user {UserId}", user.Id);

            var message = user.Status == UserStatus.Active
                ? "Email confirmed successfully. Your account is now active."
                : "Email confirmed successfully. Your account is pending approval by staff.";

            var data = new
            {
                userId = user.Id,
                email = user.Email,
                status = user.Status.ToString(),
                requiresApproval = user.Status == UserStatus.PendingApproval
            };

            return new ResponseModel(HttpStatusCode.OK, message, data);
        }, cancellationToken);
    }
}