using System.Text.Json;
using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClassroomService.Application.Features.SupportRequests.Commands;

/// <summary>
/// Handler for removing a specific image attachment from a support request
/// </summary>
public class RemoveSupportRequestImageCommandHandler : IRequestHandler<RemoveSupportRequestImageCommand, RemoveSupportRequestImageResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUploadService _uploadService;
    private readonly ILogger<RemoveSupportRequestImageCommandHandler> _logger;

    public RemoveSupportRequestImageCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IUploadService uploadService,
        ILogger<RemoveSupportRequestImageCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _uploadService = uploadService;
        _logger = logger;
    }

    public async Task<RemoveSupportRequestImageResponse> Handle(RemoveSupportRequestImageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate user is authenticated
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue || currentUserId.Value == Guid.Empty)
            {
                return new RemoveSupportRequestImageResponse
                {
                    Success = false,
                    Message = "User not authenticated"
                };
            }

            var userId = currentUserId.Value;

            // Validate image URL is provided
            if (string.IsNullOrWhiteSpace(request.ImageUrl))
            {
                return new RemoveSupportRequestImageResponse
                {
                    Success = false,
                    Message = "Image URL is required"
                };
            }

            // Get support request
            var supportRequest = await _unitOfWork.SupportRequests.GetAsync(
                sr => sr.Id == request.SupportRequestId,
                cancellationToken);

            if (supportRequest == null)
            {
                return new RemoveSupportRequestImageResponse
                {
                    Success = false,
                    Message = "Support request not found"
                };
            }

            // Check if user is the requester
            if (supportRequest.RequesterId != userId)
            {
                return new RemoveSupportRequestImageResponse
                {
                    Success = false,
                    Message = "Only the requester can remove images from their support request"
                };
            }

            // Deserialize existing images
            if (string.IsNullOrEmpty(supportRequest.Images))
            {
                return new RemoveSupportRequestImageResponse
                {
                    Success = false,
                    Message = "No images found in this support request"
                };
            }

            var existingImages = JsonSerializer.Deserialize<List<string>>(supportRequest.Images) ?? new List<string>();

            // Check if image exists
            if (!existingImages.Contains(request.ImageUrl))
            {
                return new RemoveSupportRequestImageResponse
                {
                    Success = false,
                    Message = "Image not found in this support request"
                };
            }

            // Delete the image from storage
            try
            {
                await _uploadService.DeleteFileAsync(request.ImageUrl);
                _logger.LogInformation(
                    "Deleted image {ImageUrl} from SupportRequest {SupportRequestId}",
                    request.ImageUrl,
                    request.SupportRequestId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete image from storage: {ImageUrl}", request.ImageUrl);
                // Continue with removing from metadata even if storage deletion fails
            }

            // Remove from images list
            existingImages.Remove(request.ImageUrl);

            // Save updated images JSON (or null if no images remain)
            supportRequest.Images = existingImages.Any() ? JsonSerializer.Serialize(existingImages) : null;
            await _unitOfWork.SupportRequests.UpdateAsync(supportRequest, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new RemoveSupportRequestImageResponse
            {
                Success = true,
                Message = "Successfully removed image"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing support request image from SupportRequest {SupportRequestId}", 
                request.SupportRequestId);
            return new RemoveSupportRequestImageResponse
            {
                Success = false,
                Message = $"An error occurred while removing the image: {ex.Message}"
            };
        }
    }
}
