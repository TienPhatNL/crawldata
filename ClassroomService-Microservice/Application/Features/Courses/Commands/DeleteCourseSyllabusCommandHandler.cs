using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClassroomService.Application.Features.Courses.Commands;

/// <summary>
/// Handler for deleting course syllabus file
/// Only the course lecturer can delete the syllabus
/// </summary>
public class DeleteCourseSyllabusCommandHandler : IRequestHandler<DeleteCourseSyllabusCommand, DeleteCourseSyllabusResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUploadService _uploadService;
    private readonly ILogger<DeleteCourseSyllabusCommandHandler> _logger;

    public DeleteCourseSyllabusCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IUploadService uploadService,
        ILogger<DeleteCourseSyllabusCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _uploadService = uploadService;
        _logger = logger;
    }

    public async Task<DeleteCourseSyllabusResponse> Handle(DeleteCourseSyllabusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate user is authenticated
            var currentUserId = _currentUserService.UserId;
            var userRole = _currentUserService.Role;

            if (!currentUserId.HasValue || currentUserId.Value == Guid.Empty)
            {
                return new DeleteCourseSyllabusResponse
                {
                    Success = false,
                    Message = "User not authenticated"
                };
            }

            // Only lecturers can delete syllabus
            if (userRole != RoleConstants.Lecturer)
            {
                return new DeleteCourseSyllabusResponse
                {
                    Success = false,
                    Message = "Only lecturers can delete course syllabus"
                };
            }

            // Get course
            var course = await _unitOfWork.Courses.GetAsync(
                c => c.Id == request.CourseId,
                cancellationToken);

            if (course == null)
            {
                return new DeleteCourseSyllabusResponse
                {
                    Success = false,
                    Message = "Course not found"
                };
            }

            // Check if user is the course lecturer
            if (course.LecturerId != currentUserId.Value)
            {
                return new DeleteCourseSyllabusResponse
                {
                    Success = false,
                    Message = "You do not have permission to delete syllabus for this course"
                };
            }

            // Check if syllabus file exists
            if (string.IsNullOrEmpty(course.SyllabusFile))
            {
                return new DeleteCourseSyllabusResponse
                {
                    Success = false,
                    Message = "No syllabus file to delete"
                };
            }

            // Delete file from storage
            try
            {
                await _uploadService.DeleteFileAsync(course.SyllabusFile);
                _logger.LogInformation("Deleted syllabus file: {FileUrl}", course.SyllabusFile);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete syllabus file from storage: {FileUrl}", course.SyllabusFile);
            }

            // Update course
            course.SyllabusFile = null;
            await _unitOfWork.Courses.UpdateAsync(course, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Syllabus file deleted successfully for course {CourseId}", course.Id);

            return new DeleteCourseSyllabusResponse
            {
                Success = true,
                Message = "Syllabus file deleted successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting syllabus file for course {CourseId}", request.CourseId);
            return new DeleteCourseSyllabusResponse
            {
                Success = false,
                Message = $"Error deleting syllabus file: {ex.Message}"
            };
        }
    }
}
