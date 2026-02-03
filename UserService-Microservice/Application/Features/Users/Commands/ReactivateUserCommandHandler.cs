using System.Net;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.Common.Models;
using UserService.Domain.Enums;
using UserService.Domain.Events;
using UserService.Infrastructure.Messaging;
using UserService.Infrastructure.Repositories;

namespace UserService.Application.Features.Users.Commands;

public class ReactivateUserCommandHandler : IRequestHandler<ReactivateUserCommand, ResponseModel>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReactivateUserCommandHandler> _logger;
    private readonly CacheInvalidationPublisher _cacheInvalidationPublisher;

    public ReactivateUserCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<ReactivateUserCommandHandler> logger,
        CacheInvalidationPublisher cacheInvalidationPublisher)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cacheInvalidationPublisher = cacheInvalidationPublisher;
    }

    public async Task<ResponseModel> Handle(ReactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null || user.IsDeleted)
        {
            _logger.LogWarning("Reactivation attempted for non-existent user {UserId}", request.UserId);
            throw new ValidationException("User not found");
        }

        var reactivatedByUser = await _unitOfWork.Users.GetByIdAsync(request.ReactivatedById, cancellationToken);
        if (reactivatedByUser == null)
        {
            _logger.LogWarning("Invalid reactivated by user {ReactivatedById}", request.ReactivatedById);
            throw new ValidationException("Invalid staff user");
        }

        // Check permissions - only Admin and Staff can reactivate users
        if (reactivatedByUser.Role != UserRole.Admin && reactivatedByUser.Role != UserRole.Staff)
        {
            _logger.LogWarning("Unauthorized reactivation attempt by user {ReactivatedById} with role {Role}",
                request.ReactivatedById, reactivatedByUser.Role);
            throw new ValidationException("Insufficient permissions to reactivate users");
        }

        // Check if user can be reactivated
        if (user.Status != UserStatus.Suspended && user.Status != UserStatus.Inactive)
        {
            _logger.LogWarning("Attempted to reactivate user {UserId} with status {Status}", user.Id, user.Status);
            throw new ValidationException($"User with status '{user.Status}' cannot be reactivated");
        }

        // Reactivate user
        user.Status = UserStatus.Active;
        user.SuspensionReason = null;
        user.SuspendedAt = null;
        user.SuspendedById = null;
        user.SuspendedUntil = null;
        user.ReactivatedAt = DateTime.UtcNow;
        user.ReactivatedById = request.ReactivatedById;
        user.UpdatedAt = DateTime.UtcNow;

        if (!user.IsEmailConfirmed)
        {
            user.EmailConfirmedAt = DateTime.UtcNow;
        }

        // Add domain event (will be auto-dispatched by SaveChangesAsync)
        user.AddDomainEvent(new UserReactivatedEvent(user.Id, user.Email, request.ReactivatedById));

        await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} reactivated by {ReactivatedById}", user.Id, request.ReactivatedById);

        // Invalidate cache
        await _cacheInvalidationPublisher.PublishUserInvalidationAsync(
            user.Id,
            InvalidationType.StatusChanged,
            "User reactivated",
            cancellationToken);

        return new ResponseModel(HttpStatusCode.OK, "User reactivated successfully");
    }
}