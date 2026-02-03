using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.Courses.Commands;

public class InactivateCourseCommandHandler : IRequestHandler<InactivateCourseCommand, InactivateCourseResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUploadService _uploadService;
    private readonly ILogger<InactivateCourseCommandHandler> _logger;

    public InactivateCourseCommandHandler(
        IUnitOfWork unitOfWork,
        IUploadService uploadService,
        ILogger<InactivateCourseCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _uploadService = uploadService;
        _logger = logger;
    }

    public async Task<InactivateCourseResponse> Handle(InactivateCourseCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Find the course
            var course = await _unitOfWork.Courses.GetCourseWithDetailsAsync(request.CourseId, cancellationToken);

            if (course == null)
            {
                _logger.LogWarning("Course not found: {CourseId}", request.CourseId);
                return new InactivateCourseResponse
                {
                    Success = false,
                    Message = "Course not found",
                    CourseId = null
                };
            }

            // Verify the lecturer owns this course
            if (course.LecturerId != request.LecturerId)
            {
                _logger.LogWarning("Lecturer {LecturerId} attempted to delete course {CourseId} owned by {OwnerId}",
                    request.LecturerId, request.CourseId, course.LecturerId);
                return new InactivateCourseResponse
                {
                    Success = false,
                    Message = "You can only delete your own courses",
                    CourseId = null
                };
            }

            // Only allow deletion if course is in PendingApproval status
            if (course.Status != CourseStatus.PendingApproval)
            {
                _logger.LogWarning("Cannot delete course {CourseId} - Status is {Status}, must be PendingApproval",
                    request.CourseId, course.Status);
                return new InactivateCourseResponse
                {
                    Success = false,
                    Message = $"Cannot delete course. Only courses with PendingApproval status can be deleted. Current status: {course.Status}",
                    CourseId = null
                };
            }

            // Delete course image from S3 if exists
            if (!string.IsNullOrEmpty(course.Img))
            {
                try
                {
                    await _uploadService.DeleteFileAsync(course.Img);
                    _logger.LogInformation("Deleted course image {ImageUrl} for course {CourseId}",
                        course.Img, course.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete course image from S3: {ImageUrl}", course.Img);
                    // Continue with deletion even if S3 deletion fails
                }
            }

            // Delete syllabus file from S3 if exists
            if (!string.IsNullOrEmpty(course.SyllabusFile))
            {
                try
                {
                    await _uploadService.DeleteFileAsync(course.SyllabusFile);
                    _logger.LogInformation("Deleted syllabus file {SyllabusFile} for course {CourseId}",
                        course.SyllabusFile, course.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete syllabus file from S3: {SyllabusFile}", course.SyllabusFile);
                    // Continue with deletion even if S3 deletion fails
                }
            }

            // Hard delete the course
            await _unitOfWork.Courses.DeleteAsync(course, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(request.Reason))
            {
                _logger.LogInformation("Course {CourseId} ({CourseName}) hard deleted by lecturer {LecturerId}. Reason: {Reason}",
                    course.Id, course.Name, request.LecturerId, request.Reason);
            }
            else
            {
                _logger.LogInformation("Course {CourseId} ({CourseName}) hard deleted by lecturer {LecturerId}",
                    course.Id, course.Name, request.LecturerId);
            }

            return new InactivateCourseResponse
            {
                Success = true,
                Message = "Course deleted successfully",
                CourseId = course.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inactivating course {CourseId}", request.CourseId);
            return new InactivateCourseResponse
            {
                Success = false,
                Message = "An error occurred while inactivating the course",
                CourseId = null
            };
        }
    }
}
