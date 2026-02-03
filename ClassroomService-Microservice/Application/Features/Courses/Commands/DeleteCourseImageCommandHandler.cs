using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClassroomService.Application.Features.Courses.Commands;

/// <summary>
/// Handler for deleting course images
/// </summary>
public class DeleteCourseImageCommandHandler : IRequestHandler<DeleteCourseImageCommand, DeleteCourseImageResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUploadService _uploadService;
    private readonly ILogger<DeleteCourseImageCommandHandler> _logger;

    public DeleteCourseImageCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IUploadService uploadService,
        ILogger<DeleteCourseImageCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _uploadService = uploadService;
        _logger = logger;
    }

    public async Task<DeleteCourseImageResponse> Handle(DeleteCourseImageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate user is authenticated
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue || currentUserId.Value == Guid.Empty)
            {
                return new DeleteCourseImageResponse
                {
                    Success = false,
                    Message = "User not authenticated"
                };
            }

            // Get course
            var course = await _unitOfWork.Courses.GetAsync(
                c => c.Id == request.CourseId,
                cancellationToken);

            if (course == null)
            {
                return new DeleteCourseImageResponse
                {
                    Success = false,
                    Message = "Course not found"
                };
            }

            // Check if user is the lecturer
            if (course.LecturerId != currentUserId.Value)
            {
                return new DeleteCourseImageResponse
                {
                    Success = false,
                    Message = "Only the course lecturer can delete course images"
                };
            }

            // Check if there's an image to delete
            if (string.IsNullOrEmpty(course.Img))
            {
                return new DeleteCourseImageResponse
                {
                    Success = false,
                    Message = "Course does not have an image to delete"
                };
            }

            // Delete from S3
            try
            {
                await _uploadService.DeleteFileAsync(course.Img);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete course image from S3: {ImageUrl}", course.Img);
                return new DeleteCourseImageResponse
                {
                    Success = false,
                    Message = "Failed to delete image from storage"
                };
            }

            // Update course
            course.Img = null;
            await _unitOfWork.Courses.UpdateAsync(course, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Course image deleted successfully for Course {CourseId} by User {UserId}",
                request.CourseId,
                currentUserId.Value);

            return new DeleteCourseImageResponse
            {
                Success = true,
                Message = "Course image deleted successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting course image for Course {CourseId}", request.CourseId);
            return new DeleteCourseImageResponse
            {
                Success = false,
                Message = $"An error occurred while deleting the image: {ex.Message}"
            };
        }
    }
}
