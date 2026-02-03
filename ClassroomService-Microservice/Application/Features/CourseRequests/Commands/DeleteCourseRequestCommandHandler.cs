using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClassroomService.Application.Features.CourseRequests.Commands;

/// <summary>
/// Handler for deleting course requests (hard delete)
/// Only the requesting lecturer can delete, and only if status is Pending
/// </summary>
public class DeleteCourseRequestCommandHandler : IRequestHandler<DeleteCourseRequestCommand, DeleteCourseRequestResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUploadService _uploadService;
    private readonly ILogger<DeleteCourseRequestCommandHandler> _logger;

    public DeleteCourseRequestCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IUploadService uploadService,
        ILogger<DeleteCourseRequestCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _uploadService = uploadService;
        _logger = logger;
    }

    public async Task<DeleteCourseRequestResponse> Handle(DeleteCourseRequestCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get the course request
            var courseRequest = await _unitOfWork.CourseRequests.GetAsync(
                cr => cr.Id == request.CourseRequestId,
                cancellationToken);

            if (courseRequest == null)
            {
                _logger.LogWarning("Course request not found: {CourseRequestId}", request.CourseRequestId);
                return new DeleteCourseRequestResponse
                {
                    Success = false,
                    Message = "Course request not found"
                };
            }

            // Check if the current user is the owner of the course request
            if (courseRequest.LecturerId != request.LecturerId)
            {
                _logger.LogWarning("Lecturer {LecturerId} attempted to delete course request {CourseRequestId} owned by {OwnerId}",
                    request.LecturerId, request.CourseRequestId, courseRequest.LecturerId);
                return new DeleteCourseRequestResponse
                {
                    Success = false,
                    Message = "You are not authorized to delete this course request"
                };
            }

            // Check if the course request is in Pending state
            if (courseRequest.Status != CourseRequestStatus.Pending)
            {
                _logger.LogWarning("Cannot delete course request {CourseRequestId} - Status is {Status}, must be Pending",
                    request.CourseRequestId, courseRequest.Status);
                return new DeleteCourseRequestResponse
                {
                    Success = false,
                    Message = $"Cannot delete course request. Only pending requests can be deleted. Current status: {courseRequest.Status}"
                };
            }

            // Delete syllabus file from S3 if exists
            if (!string.IsNullOrEmpty(courseRequest.SyllabusFile))
            {
                try
                {
                    await _uploadService.DeleteFileAsync(courseRequest.SyllabusFile);
                    _logger.LogInformation("Deleted syllabus file {SyllabusFile} for course request {CourseRequestId}",
                        courseRequest.SyllabusFile, courseRequest.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete syllabus file from S3: {SyllabusFile}", courseRequest.SyllabusFile);
                    // Continue with deletion even if S3 deletion fails
                }
            }

            // Hard delete the course request
            await _unitOfWork.CourseRequests.DeleteAsync(courseRequest, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Course request {CourseRequestId} deleted by lecturer {LecturerId}",
                courseRequest.Id, request.LecturerId);

            return new DeleteCourseRequestResponse
            {
                Success = true,
                Message = "Course request deleted successfully",
                CourseRequestId = courseRequest.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting course request {CourseRequestId}", request.CourseRequestId);
            return new DeleteCourseRequestResponse
            {
                Success = false,
                Message = "An error occurred while deleting the course request"
            };
        }
    }
}
