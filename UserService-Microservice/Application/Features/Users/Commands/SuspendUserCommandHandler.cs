using System.Net;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.Common.Models;
using UserService.Domain.Entities;
using UserService.Domain.Enums;
using UserService.Domain.Events;
using UserService.Infrastructure.Messaging;
using UserService.Infrastructure.Repositories;
using UserService.Infrastructure.Services;

namespace UserService.Application.Features.Users.Commands;

public class SuspendUserCommandHandler : IRequestHandler<SuspendUserCommand, ResponseModel>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SuspendUserCommandHandler> _logger;
    private readonly IEmailService _emailService;
    private readonly CacheInvalidationPublisher _cacheInvalidationPublisher;

    public SuspendUserCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<SuspendUserCommandHandler> logger,
        IEmailService emailService,
        CacheInvalidationPublisher cacheInvalidationPublisher)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _emailService = emailService;
        _cacheInvalidationPublisher = cacheInvalidationPublisher;
    }

    public async Task<ResponseModel> Handle(SuspendUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null || user.IsDeleted)
        {
            _logger.LogWarning("Suspension attempted for non-existent user {UserId}", request.UserId);
            throw new ValidationException("User not found");
        }

        var suspendedByUser = await _unitOfWork.Users.GetByIdAsync(request.SuspendedById, cancellationToken);
        if (suspendedByUser == null)
        {
            _logger.LogWarning("Invalid suspended by user {SuspendedById}", request.SuspendedById);
            throw new ValidationException("Invalid staff user");
        }

        // Check permissions - only Admin and Staff can suspend users
        if (suspendedByUser.Role != UserRole.Admin && suspendedByUser.Role != UserRole.Staff)
        {
            _logger.LogWarning("Unauthorized suspension attempt by user {SuspendedById} with role {Role}",
                request.SuspendedById, suspendedByUser.Role);
            throw new ValidationException("Insufficient permissions to suspend users");
        }

        // Don't allow suspending Admin users unless suspended by another Admin
        if (user.Role == UserRole.Admin && suspendedByUser.Role != UserRole.Admin)
        {
            _logger.LogWarning("Attempted to suspend Admin user {UserId} by non-Admin {SuspendedById}",
                request.UserId, request.SuspendedById);
            throw new ValidationException("Only Admin users can suspend other Admin users");
        }

        // Update user status
        user.Status = UserStatus.Suspended;
        user.SuspensionReason = request.Reason;
        user.SuspendedAt = DateTime.UtcNow;
        user.SuspendedById = request.SuspendedById;
        user.SuspendedUntil = request.SuspendUntil;
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Users.UpdateAsync(user, cancellationToken);

        // Invalidate all active user sessions
        var userSessions = await _unitOfWork.UserSessions.GetManyAsync(s => s.UserId == user.Id && s.IsActive, cancellationToken);
        foreach (var session in userSessions)
        {
            session.IsActive = false;
            session.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.UserSessions.UpdateAsync(session, cancellationToken);
        }

        // Add domain event (will be auto-dispatched by SaveChangesAsync)
        user.AddDomainEvent(new UserSuspendedEvent(user.Id, user.Email, request.Reason, request.SuspendedById));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} suspended by {SuspendedById} with reason: {Reason}",
            user.Id, request.SuspendedById, request.Reason);

        // Invalidate cache
        await _cacheInvalidationPublisher.PublishUserInvalidationAsync(
            user.Id,
            InvalidationType.StatusChanged,
            "User suspended",
            cancellationToken);

        try
        {
            await _emailService.SendAccountSuspendedEmailAsync(
                user.Email,
                user.FullName,
                request.Reason ?? "Your account has been suspended",
                cancellationToken);

            _logger.LogInformation("Suspension email sent to {UserId} ({Email})", user.Id, user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send suspension email to {UserId} ({Email})", user.Id, user.Email);
        }

        var message = $"User suspended successfully{(request.SuspendUntil.HasValue ? $" until {request.SuspendUntil:yyyy-MM-dd}" : "")}";
        return new ResponseModel(HttpStatusCode.OK, message);
    }
}