using System.Net;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.Common.Models;
using UserService.Domain.Events;
using UserService.Infrastructure.Messaging;
using UserService.Infrastructure.Repositories;

namespace UserService.Application.Features.Users.Commands;

public class UpdateUserProfileCommandHandler : IRequestHandler<UpdateUserProfileCommand, ResponseModel>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateUserProfileCommandHandler> _logger;
    private readonly CacheInvalidationPublisher _cacheInvalidationPublisher;

    public UpdateUserProfileCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<UpdateUserProfileCommandHandler> logger,
        CacheInvalidationPublisher cacheInvalidationPublisher)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cacheInvalidationPublisher = cacheInvalidationPublisher;
    }

    public async Task<ResponseModel> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        
        if (user == null || user.IsDeleted)
        {
            _logger.LogWarning("Profile update attempted for non-existent user {UserId}", request.UserId);
            throw new ValidationException("User not found");
        }

        return await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            // Update user profile fields
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.InstitutionName = request.InstitutionName;
            user.InstitutionAddress = request.InstitutionAddress;
            user.StudentId = request.StudentId;
            user.Department = request.Department;
            user.UpdatedAt = DateTime.UtcNow;

            // Add domain event with changed fields (will be auto-dispatched by ExecuteTransactionAsync)
            var changedFields = new Dictionary<string, object>
            {
                ["FirstName"] = user.FirstName,
                ["LastName"] = user.LastName,
                ["InstitutionName"] = user.InstitutionName ?? string.Empty,
                ["Department"] = user.Department ?? string.Empty
            };
            user.AddDomainEvent(new UserProfileUpdatedEvent(user.Id, changedFields));

            await _unitOfWork.Users.UpdateAsync(user, cancellationToken);

            _logger.LogInformation("User profile updated for user {UserId}", user.Id);

            // Invalidate cache
            await _cacheInvalidationPublisher.PublishUserInvalidationAsync(
                user.Id, 
                InvalidationType.ProfileUpdated, 
                "Profile information updated",
                cancellationToken);

            var data = new
            {
                userId = user.Id,
                firstName = user.FirstName,
                lastName = user.LastName,
                institutionName = user.InstitutionName,
                institutionAddress = user.InstitutionAddress,
                studentId = user.StudentId,
                department = user.Department
            };

            return new ResponseModel(HttpStatusCode.OK, "Profile updated successfully", data);
        }, cancellationToken);
    }
}