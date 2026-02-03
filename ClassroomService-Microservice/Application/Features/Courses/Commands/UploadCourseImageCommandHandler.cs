using ClassroomService.Application.Common.Helpers;
using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClassroomService.Application.Features.Courses.Commands;

/// <summary>
/// Handler for uploading course images
/// </summary>
public class UploadCourseImageCommandHandler : IRequestHandler<UploadCourseImageCommand, UploadCourseImageResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUploadService _uploadService;
    private readonly ILogger<UploadCourseImageCommandHandler> _logger;

    public UploadCourseImageCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IUploadService uploadService,
        ILogger<UploadCourseImageCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _uploadService = uploadService;
        _logger = logger;
    }

    public async Task<UploadCourseImageResponse> Handle(UploadCourseImageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate user is authenticated
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue || currentUserId.Value == Guid.Empty)
            {
                return new UploadCourseImageResponse
                {
                    Success = false,
                    Message = "User not authenticated"
                };
            }

            // Validate file
            if (!FileValidationHelper.ValidateImageFile(request.Image, out var validationError))
            {
                return new UploadCourseImageResponse
                {
                    Success = false,
                    Message = validationError
                };
            }

            // Get course
            var course = await _unitOfWork.Courses.GetAsync(
                c => c.Id == request.CourseId,
                cancellationToken);

            if (course == null)
            {
                return new UploadCourseImageResponse
                {
                    Success = false,
                    Message = "Course not found"
                };
            }

            // Check if user is the lecturer
            if (course.LecturerId != currentUserId.Value)
            {
                return new UploadCourseImageResponse
                {
                    Success = false,
                    Message = "Only the course lecturer can upload course images"
                };
            }

            // Delete old image if exists
            if (!string.IsNullOrEmpty(course.Img))
            {
                try
                {
                    await _uploadService.DeleteFileAsync(course.Img);
                    _logger.LogInformation("Deleted old course image for Course {CourseId}", request.CourseId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old course image. Continuing with upload...");
                }
            }

            // Upload new image
            var imageUrl = await _uploadService.UploadFileAsync(request.Image);
            
            // Update course
            course.Img = imageUrl;
            await _unitOfWork.Courses.UpdateAsync(course, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Course image uploaded successfully for Course {CourseId} by User {UserId}",
                request.CourseId,
                currentUserId.Value);

            return new UploadCourseImageResponse
            {
                Success = true,
                Message = "Course image uploaded successfully",
                ImageUrl = imageUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading course image for Course {CourseId}", request.CourseId);
            return new UploadCourseImageResponse
            {
                Success = false,
                Message = $"An error occurred while uploading the image: {ex.Message}"
            };
        }
    }
}
