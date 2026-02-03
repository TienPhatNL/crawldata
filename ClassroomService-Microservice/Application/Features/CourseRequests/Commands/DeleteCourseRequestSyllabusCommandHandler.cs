using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClassroomService.Application.Features.CourseRequests.Commands;

/// <summary>
/// Handler for deleting course request syllabus file
/// Only the requesting lecturer can delete the syllabus
/// </summary>
public class DeleteCourseRequestSyllabusCommandHandler : IRequestHandler<DeleteCourseRequestSyllabusCommand, DeleteCourseRequestSyllabusResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUploadService _uploadService;
    private readonly ILogger<DeleteCourseRequestSyllabusCommandHandler> _logger;

    public DeleteCourseRequestSyllabusCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IUploadService uploadService,
        ILogger<DeleteCourseRequestSyllabusCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _uploadService = uploadService;
        _logger = logger;
    }

    public async Task<DeleteCourseRequestSyllabusResponse> Handle(DeleteCourseRequestSyllabusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate user is authenticated
            var currentUserId = _currentUserService.UserId;
            var userRole = _currentUserService.Role;

            if (!currentUserId.HasValue || currentUserId.Value == Guid.Empty)
            {
                return new DeleteCourseRequestSyllabusResponse
                {
                    Success = false,
                    Message = "User not authenticated"
                };
            }

            // Only lecturers can delete syllabus
            if (userRole != RoleConstants.Lecturer)
            {
                return new DeleteCourseRequestSyllabusResponse
                {
                    Success = false,
                    Message = "Only lecturers can delete course request syllabus"
                };
            }

            // Get course request
            var courseRequest = await _unitOfWork.CourseRequests.GetAsync(
                cr => cr.Id == request.CourseRequestId,
                cancellationToken);

            if (courseRequest == null)
            {
                return new DeleteCourseRequestSyllabusResponse
                {
                    Success = false,
                    Message = "Course request not found"
                };
            }

            // Check if user is the requesting lecturer
            if (courseRequest.LecturerId != currentUserId.Value)
            {
                return new DeleteCourseRequestSyllabusResponse
                {
                    Success = false,
                    Message = "You do not have permission to delete syllabus for this course request"
                };
            }

            // Check if syllabus file exists
            if (string.IsNullOrEmpty(courseRequest.SyllabusFile))
            {
                return new DeleteCourseRequestSyllabusResponse
                {
                    Success = false,
                    Message = "No syllabus file to delete"
                };
            }

            // Delete file from storage
            try
            {
                await _uploadService.DeleteFileAsync(courseRequest.SyllabusFile);
                _logger.LogInformation("Deleted syllabus file: {FileUrl}", courseRequest.SyllabusFile);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete syllabus file from storage: {FileUrl}", courseRequest.SyllabusFile);
            }

            // Update course request
            courseRequest.SyllabusFile = null;
            await _unitOfWork.CourseRequests.UpdateAsync(courseRequest, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Syllabus file deleted successfully for course request {CourseRequestId}", courseRequest.Id);

            return new DeleteCourseRequestSyllabusResponse
            {
                Success = true,
                Message = "Syllabus file deleted successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting syllabus file for course request {CourseRequestId}", request.CourseRequestId);
            return new DeleteCourseRequestSyllabusResponse
            {
                Success = false,
                Message = $"Error deleting syllabus file: {ex.Message}"
            };
        }
    }
}
