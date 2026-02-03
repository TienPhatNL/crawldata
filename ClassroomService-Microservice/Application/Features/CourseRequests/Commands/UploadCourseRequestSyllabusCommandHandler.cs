using ClassroomService.Application.Common.Helpers;
using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClassroomService.Application.Features.CourseRequests.Commands;

/// <summary>
/// Handler for uploading course request syllabus file
/// Only the requesting lecturer can upload the syllabus
/// </summary>
public class UploadCourseRequestSyllabusCommandHandler : IRequestHandler<UploadCourseRequestSyllabusCommand, UploadCourseRequestSyllabusResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUploadService _uploadService;
    private readonly ILogger<UploadCourseRequestSyllabusCommandHandler> _logger;

    public UploadCourseRequestSyllabusCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IUploadService uploadService,
        ILogger<UploadCourseRequestSyllabusCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _uploadService = uploadService;
        _logger = logger;
    }

    public async Task<UploadCourseRequestSyllabusResponse> Handle(UploadCourseRequestSyllabusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate user is authenticated
            var currentUserId = _currentUserService.UserId;
            var userRole = _currentUserService.Role;

            if (!currentUserId.HasValue || currentUserId.Value == Guid.Empty)
            {
                return new UploadCourseRequestSyllabusResponse
                {
                    Success = false,
                    Message = "User not authenticated"
                };
            }

            // Only lecturers can upload syllabus
            if (userRole != RoleConstants.Lecturer)
            {
                return new UploadCourseRequestSyllabusResponse
                {
                    Success = false,
                    Message = "Only lecturers can upload course request syllabus"
                };
            }

            // Validate file
            var allowedExtensions = new[] { ".pdf", ".docx", ".pptx", ".zip", ".doc", ".ppt" };
            var maxFileSize = 50 * 1024 * 1024; // 50MB

            if (request.File == null || request.File.Length == 0)
            {
                return new UploadCourseRequestSyllabusResponse
                {
                    Success = false,
                    Message = "No file provided"
                };
            }

            var fileExtension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return new UploadCourseRequestSyllabusResponse
                {
                    Success = false,
                    Message = $"File type not allowed. Allowed types: {string.Join(", ", allowedExtensions)}"
                };
            }

            if (request.File.Length > maxFileSize)
            {
                return new UploadCourseRequestSyllabusResponse
                {
                    Success = false,
                    Message = $"File size exceeds maximum allowed size of {maxFileSize / (1024 * 1024)}MB"
                };
            }

            // Get course request
            var courseRequest = await _unitOfWork.CourseRequests.GetAsync(
                cr => cr.Id == request.CourseRequestId,
                cancellationToken);

            if (courseRequest == null)
            {
                return new UploadCourseRequestSyllabusResponse
                {
                    Success = false,
                    Message = "Course request not found"
                };
            }

            // Check if user is the requesting lecturer
            if (courseRequest.LecturerId != currentUserId.Value)
            {
                return new UploadCourseRequestSyllabusResponse
                {
                    Success = false,
                    Message = "You do not have permission to upload syllabus for this course request"
                };
            }

            // Delete old syllabus file if exists
            if (!string.IsNullOrEmpty(courseRequest.SyllabusFile))
            {
                try
                {
                    await _uploadService.DeleteFileAsync(courseRequest.SyllabusFile);
                    _logger.LogInformation("Deleted old syllabus file: {OldFile}", courseRequest.SyllabusFile);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old syllabus file: {OldFile}", courseRequest.SyllabusFile);
                }
            }

            // Upload new file
            var fileUrl = await _uploadService.UploadFileAsync(request.File);

            // Update course request
            courseRequest.SyllabusFile = fileUrl;
            await _unitOfWork.CourseRequests.UpdateAsync(courseRequest, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Syllabus file uploaded successfully for course request {CourseRequestId}: {FileUrl}", 
                courseRequest.Id, fileUrl);

            return new UploadCourseRequestSyllabusResponse
            {
                Success = true,
                Message = "Syllabus file uploaded successfully",
                FileUrl = fileUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading syllabus file for course request {CourseRequestId}", request.CourseRequestId);
            return new UploadCourseRequestSyllabusResponse
            {
                Success = false,
                Message = $"Error uploading syllabus file: {ex.Message}"
            };
        }
    }
}
