using System.Net;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.Common.Models;
using UserService.Domain.Enums;
using UserService.Domain.Events;
using UserService.Infrastructure.Messaging;
using UserService.Infrastructure.Repositories;
using UserService.Infrastructure.Services;

namespace UserService.Application.Features.Users.Commands;

public class ApproveUserCommandHandler : IRequestHandler<ApproveUserCommand, ResponseModel>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ApproveUserCommandHandler> _logger;
    private readonly IEmailService _emailService;
    private readonly CacheInvalidationPublisher _cacheInvalidationPublisher;

    public ApproveUserCommandHandler(
        IUnitOfWork unitOfWork, 
        ILogger<ApproveUserCommandHandler> logger, 
        IEmailService emailService,
        CacheInvalidationPublisher cacheInvalidationPublisher)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _emailService = emailService;
        _cacheInvalidationPublisher = cacheInvalidationPublisher;
    }

    public async Task<ResponseModel> Handle(ApproveUserCommand request, CancellationToken cancellationToken)
    {
        // Verify the approving user is Staff or Admin
        var approvingUser = await _unitOfWork.Users.GetByIdAsync(request.ApprovedBy, cancellationToken);
        if (approvingUser?.Role != UserRole.Staff && approvingUser?.Role != UserRole.Admin)
        {
            throw new ValidationException("Only Staff and Admin users can approve accounts");
        }

        // Get the user to approve
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            throw new ValidationException("User not found");
        }

        if (user.Status != UserStatus.PendingApproval)
        {
            throw new ValidationException("User is not pending approval");
        }

        // Process approval
        if (request.Approved)
        {
            if (!user.IsEmailConfirmed)
            {
                user.EmailConfirmedAt = DateTime.UtcNow;
            }

            user.Status = UserStatus.Active;

            user.ApprovedAt = DateTime.UtcNow;
            user.ApprovedBy = request.ApprovedBy;
            user.ApprovalNotes = request.ApprovalNotes;

            // Add domain event for approval
            user.AddDomainEvent(new UserStatusChangedEvent(
                user.Id, 
                UserStatus.PendingApproval, 
                user.Status, 
                "Approved by staff", 
                request.ApprovedBy));

            _logger.LogInformation("User {UserId} ({Email}) approved by {ApprovedBy}",
                user.Id, user.Email, request.ApprovedBy);

            // Send approval email for lecturers
            if (user.Role == UserRole.Lecturer)
            {
                try
                {
                    await _emailService.SendUserApprovedEmailAsync(user.Email, user.FullName, cancellationToken);
                    _logger.LogInformation("Approval email sent to lecturer {UserId} ({Email})", user.Id, user.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send approval email to lecturer {UserId} ({Email})", user.Id, user.Email);
                    // Continue with approval process even if email fails
                }
            }
        }
        else
        {
            user.Status = UserStatus.Suspended;
            user.ApprovalNotes = request.RejectionReason ?? "Account rejected by staff";

            // Add domain event for rejection
            user.AddDomainEvent(new UserStatusChangedEvent(
                user.Id,
                UserStatus.PendingApproval,
                UserStatus.Suspended,
                user.ApprovalNotes,
                request.ApprovedBy));

            _logger.LogInformation("User {UserId} ({Email}) rejected by {ApprovedBy}. Reason: {Reason}",
                user.Id, user.Email, request.ApprovedBy, request.RejectionReason);
        }

        await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        var invalidationType = request.Approved ? InvalidationType.StatusChanged : InvalidationType.UserUpdated;
        var reason = request.Approved ? "User approved" : "User rejected";
        await _cacheInvalidationPublisher.PublishUserInvalidationAsync(
            user.Id,
            invalidationType,
            reason,
            cancellationToken);

        var message = request.Approved
            ? "User account approved successfully"
            : "User account rejected";

        var data = new
        {
            userId = user.Id,
            userEmail = user.Email,
            status = user.Status.ToString()
        };

        return new ResponseModel(HttpStatusCode.OK, message, data);
    }
}