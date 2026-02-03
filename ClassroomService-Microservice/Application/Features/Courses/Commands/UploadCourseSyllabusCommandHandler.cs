using ClassroomService.Application.Common.Helpers;
using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClassroomService.Application.Features.Courses.Commands;

/// <summary>
/// Handler for uploading course syllabus file
/// Only the course lecturer can upload the syllabus
/// </summary>
public class UploadCourseSyllabusCommandHandler : IRequestHandler<UploadCourseSyllabusCommand, UploadCourseSyllabusResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUploadService _uploadService;
    private readonly ILogger<UploadCourseSyllabusCommandHandler> _logger;

    public UploadCourseSyllabusCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IUploadService uploadService,
        ILogger<UploadCourseSyllabusCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _uploadService = uploadService;
        _logger = logger;
    }

    public async Task<UploadCourseSyllabusResponse> Handle(UploadCourseSyllabusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate user is authenticated
            var currentUserId = _currentUserService.UserId;
            var userRole = _currentUserService.Role;

            if (!currentUserId.HasValue || currentUserId.Value == Guid.Empty)
            {
                return new UploadCourseSyllabusResponse
                {
                    Success = false,
                    Message = "User not authenticated"
                };
            }

            // Only lecturers can upload syllabus
            if (userRole != RoleConstants.Lecturer)
            {
                return new UploadCourseSyllabusResponse
                {
                    Success = false,
                    Message = "Only lecturers can upload course syllabus"
                };
            }

            // Validate file
            var allowedExtensions = new[] { ".pdf", ".docx", ".pptx", ".zip", ".doc", ".ppt" };
            var maxFileSize = 50 * 1024 * 1024; // 50MB

            if (request.File == null || request.File.Length == 0)
            {
                return new UploadCourseSyllabusResponse
                {
                    Success = false,
                    Message = "No file provided"
                };
            }

            var fileExtension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return new UploadCourseSyllabusResponse
                {
                    Success = false,
                    Message = $"File type not allowed. Allowed types: {string.Join(", ", allowedExtensions)}"
                };
            }

            if (request.File.Length > maxFileSize)
            {
                return new UploadCourseSyllabusResponse
                {
                    Success = false,
                    Message = $"File size exceeds maximum allowed size of {maxFileSize / (1024 * 1024)}MB"
                };
            }

            // Get course
            var course = await _unitOfWork.Courses.GetAsync(
                c => c.Id == request.CourseId,
                cancellationToken);

            if (course == null)
            {
                return new UploadCourseSyllabusResponse
                {
                    Success = false,
                    Message = "Course not found"
                };
            }

            // Check if user is the course lecturer
            if (course.LecturerId != currentUserId.Value)
            {
                return new UploadCourseSyllabusResponse
                {
                    Success = false,
                    Message = "You do not have permission to upload syllabus for this course"
                };
            }

            // Delete old syllabus file if exists
            if (!string.IsNullOrEmpty(course.SyllabusFile))
            {
                try
                {
                    await _uploadService.DeleteFileAsync(course.SyllabusFile);
                    _logger.LogInformation("Deleted old syllabus file: {OldFile}", course.SyllabusFile);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old syllabus file: {OldFile}", course.SyllabusFile);
                }
            }

            // Upload new file
            var fileUrl = await _uploadService.UploadFileAsync(request.File);

            // Update course
            course.SyllabusFile = fileUrl;
            await _unitOfWork.Courses.UpdateAsync(course, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Syllabus file uploaded successfully for course {CourseId}: {FileUrl}", 
                course.Id, fileUrl);

            return new UploadCourseSyllabusResponse
            {
                Success = true,
                Message = "Syllabus file uploaded successfully",
                FileUrl = fileUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading syllabus file for course {CourseId}", request.CourseId);
            return new UploadCourseSyllabusResponse
            {
                Success = false,
                Message = $"Error uploading syllabus file: {ex.Message}"
            };
        }
    }
}
