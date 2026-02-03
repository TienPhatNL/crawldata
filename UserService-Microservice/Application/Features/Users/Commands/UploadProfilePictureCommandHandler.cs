using System.Net;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.Common.Models;
using UserService.Domain.Events;
using UserService.Domain.Interfaces;
using UserService.Infrastructure.Messaging;
using UserService.Infrastructure.Repositories;

namespace UserService.Application.Features.Users.Commands;

public class UploadProfilePictureCommandHandler : IRequestHandler<UploadProfilePictureCommand, ResponseModel>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUploadService _uploadService;
    private readonly CacheInvalidationPublisher _cacheInvalidationPublisher;
    private readonly ILogger<UploadProfilePictureCommandHandler> _logger;

    public UploadProfilePictureCommandHandler(
        IUnitOfWork unitOfWork,
        IUploadService uploadService,
        CacheInvalidationPublisher cacheInvalidationPublisher,
        ILogger<UploadProfilePictureCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _uploadService = uploadService;
        _cacheInvalidationPublisher = cacheInvalidationPublisher;
        _logger = logger;
    }

    public async Task<ResponseModel> Handle(UploadProfilePictureCommand request, CancellationToken cancellationToken)
    {
        if (request.ProfilePicture == null)
        {
            throw new ValidationException("Profile picture file is required");
        }

        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null || user.IsDeleted)
        {
            _logger.LogWarning("Profile picture upload attempted for non-existent user {UserId}", request.UserId);
            throw new ValidationException("User not found");
        }

        return await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            string? previousImage = user.ProfilePictureUrl;
            string? newImageUrl;

            try
            {
                if (!string.IsNullOrEmpty(previousImage))
                {
                    await _uploadService.DeleteFileAsync(previousImage, cancellationToken);
                }

                newImageUrl = await _uploadService.UploadFileAsync(request.ProfilePicture, cancellationToken);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation failed while uploading profile picture for {UserId}", user.Id);
                throw new ValidationException(ex.Message);
            }

            user.ProfilePictureUrl = newImageUrl;
            user.UpdatedAt = DateTime.UtcNow;

            var changedFields = new Dictionary<string, object>
            {
                ["ProfilePictureUrl"] = newImageUrl ?? string.Empty
            };
            user.AddDomainEvent(new UserProfileUpdatedEvent(user.Id, changedFields));

            await _unitOfWork.Users.UpdateAsync(user, cancellationToken);

            _logger.LogInformation("Profile picture updated for user {UserId}", user.Id);

            await _cacheInvalidationPublisher.PublishUserInvalidationAsync(
                user.Id,
                InvalidationType.ProfileUpdated,
                "Profile picture updated",
                cancellationToken);

            var data = new
            {
                userId = user.Id,
                profilePictureUrl = user.ProfilePictureUrl
            };

            return new ResponseModel(HttpStatusCode.OK, "Profile picture updated successfully", data);
        }, cancellationToken);
    }
}
