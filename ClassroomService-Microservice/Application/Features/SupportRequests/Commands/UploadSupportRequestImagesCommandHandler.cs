using System.Text.Json;
using ClassroomService.Application.Common.Helpers;
using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClassroomService.Application.Features.SupportRequests.Commands;

/// <summary>
/// Handler for uploading multiple image attachments to support requests
/// </summary>
public class UploadSupportRequestImagesCommandHandler : IRequestHandler<UploadSupportRequestImagesCommand, UploadSupportRequestImagesResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUploadService _uploadService;
    private readonly ILogger<UploadSupportRequestImagesCommandHandler> _logger;

    public UploadSupportRequestImagesCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IUploadService uploadService,
        ILogger<UploadSupportRequestImagesCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _uploadService = uploadService;
        _logger = logger;
    }

    public async Task<UploadSupportRequestImagesResponse> Handle(UploadSupportRequestImagesCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate user is authenticated
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue || currentUserId.Value == Guid.Empty)
            {
                return new UploadSupportRequestImagesResponse
                {
                    Success = false,
                    Message = "User not authenticated"
                };
            }

            var userId = currentUserId.Value;

            // Validate at least one image
            if (request.Images == null || !request.Images.Any())
            {
                return new UploadSupportRequestImagesResponse
                {
                    Success = false,
                    Message = "No images provided for upload"
                };
            }

            // Get support request
            var supportRequest = await _unitOfWork.SupportRequests.GetAsync(
                sr => sr.Id == request.SupportRequestId,
                cancellationToken);

            if (supportRequest == null)
            {
                return new UploadSupportRequestImagesResponse
                {
                    Success = false,
                    Message = "Support request not found"
                };
            }

            // Check existing image count
            var existingImageCount = 0;
            if (!string.IsNullOrEmpty(supportRequest.Images))
            {
                var currentImages = JsonSerializer.Deserialize<List<string>>(supportRequest.Images);
                existingImageCount = currentImages?.Count ?? 0;
            }

            // Validate total image count (existing + new) doesn't exceed 5
            var totalImageCount = existingImageCount + request.Images.Count;
            if (totalImageCount > 5)
            {
                return new UploadSupportRequestImagesResponse
                {
                    Success = false,
                    Message = $"Cannot upload {request.Images.Count} image(s). Support request already has {existingImageCount} image(s). Maximum 5 images allowed per support request."
                };
            }

            // Re-get support request reference after validation
            supportRequest = await _unitOfWork.SupportRequests.GetAsync(
                sr => sr.Id == request.SupportRequestId,
                cancellationToken);

            if (supportRequest == null)
            {
                return new UploadSupportRequestImagesResponse
                {
                    Success = false,
                    Message = "Support request not found"
                };
            }

            // Check if user is the requester
            if (supportRequest.RequesterId != userId)
            {
                return new UploadSupportRequestImagesResponse
                {
                    Success = false,
                    Message = "Only the requester can upload images to their support request"
                };
            }

            // Validate all images first
            foreach (var image in request.Images)
            {
                if (!FileValidationHelper.ValidateImageFile(image, out var validationError))
                {
                    return new UploadSupportRequestImagesResponse
                    {
                        Success = false,
                        Message = $"Image '{image.FileName}': {validationError}"
                    };
                }
            }

            // Deserialize existing images
            var existingImages = string.IsNullOrEmpty(supportRequest.Images)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(supportRequest.Images) ?? new List<string>();

            var uploadedImageUrls = new List<string>();

            // Upload each image
            foreach (var image in request.Images)
            {
                try
                {
                    var imageUrl = await _uploadService.UploadFileAsync(image);
                    existingImages.Add(imageUrl);
                    uploadedImageUrls.Add(imageUrl);

                    _logger.LogInformation(
                        "Uploaded image {FileName} to SupportRequest {SupportRequestId} by User {UserId}",
                        image.FileName,
                        request.SupportRequestId,
                        userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to upload image {FileName}", image.FileName);
                    
                    // Clean up successfully uploaded images if any upload fails
                    foreach (var uploadedUrl in uploadedImageUrls)
                    {
                        try
                        {
                            await _uploadService.DeleteFileAsync(uploadedUrl);
                        }
                        catch (Exception deleteEx)
                        {
                            _logger.LogWarning(deleteEx, "Failed to clean up uploaded image {ImageUrl}", uploadedUrl);
                        }
                    }
                    
                    return new UploadSupportRequestImagesResponse
                    {
                        Success = false,
                        Message = $"Failed to upload image '{image.FileName}': {ex.Message}"
                    };
                }
            }

            // Save updated images JSON
            supportRequest.Images = JsonSerializer.Serialize(existingImages);
            await _unitOfWork.SupportRequests.UpdateAsync(supportRequest, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new UploadSupportRequestImagesResponse
            {
                Success = true,
                Message = $"Successfully uploaded {uploadedImageUrls.Count} image(s)",
                UploadedImageUrls = uploadedImageUrls,
                UploadedCount = uploadedImageUrls.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading support request images for SupportRequest {SupportRequestId}", request.SupportRequestId);
            return new UploadSupportRequestImagesResponse
            {
                Success = false,
                Message = $"An error occurred while uploading images: {ex.Message}"
            };
        }
    }
}
