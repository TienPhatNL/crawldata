using MediatR;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Application.Features.Courses.Commands;

public class UpdateCourseAccessCodeCommandHandler : IRequestHandler<UpdateCourseAccessCodeCommand, UpdateCourseAccessCodeResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAccessCodeService _accessCodeService;
    private readonly ILogger<UpdateCourseAccessCodeCommandHandler> _logger;

    public UpdateCourseAccessCodeCommandHandler(
        IUnitOfWork unitOfWork, 
        IAccessCodeService accessCodeService,
        ILogger<UpdateCourseAccessCodeCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _accessCodeService = accessCodeService;
        _logger = logger;
    }

    public async Task<UpdateCourseAccessCodeResponse> Handle(UpdateCourseAccessCodeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var course = await _unitOfWork.Courses
                .GetAsync(c => c.Id == request.CourseId, cancellationToken);

            if (course == null)
            {
                return new UpdateCourseAccessCodeResponse
                {
                    Success = false,
                    Message = "Course not found"
                };
            }

            // Verify lecturer ownership
            if (course.LecturerId != request.LecturerId)
            {
                return new UpdateCourseAccessCodeResponse
                {
                    Success = false,
                    Message = "You can only modify access codes for your own courses"
                };
            }

            // Update access code requirement
            course.RequiresAccessCode = request.RequiresAccessCode;

            if (request.RequiresAccessCode)
            {
                // Generate or update access code
                if (request.RegenerateCode || string.IsNullOrEmpty(course.AccessCode))
                {
                    if (request.AccessCodeType == AccessCodeType.Custom)
                    {
                        if (string.IsNullOrWhiteSpace(request.CustomAccessCode))
                        {
                            return new UpdateCourseAccessCodeResponse
                            {
                                Success = false,
                                Message = "Custom access code is required when AccessCodeType is Custom"
                            };
                        }

                        if (!_accessCodeService.IsValidAccessCodeFormat(request.CustomAccessCode, AccessCodeType.Custom))
                        {
                            return new UpdateCourseAccessCodeResponse
                            {
                                Success = false,
                                Message = "Invalid custom access code format"
                            };
                        }

                        course.AccessCode = request.CustomAccessCode;
                    }
                    else
                    {
                        course.AccessCode = _accessCodeService.GenerateAccessCode(request.AccessCodeType ?? AccessCodeType.AlphaNumeric);
                    }

                    course.AccessCodeCreatedAt = DateTime.UtcNow;
                    course.AccessCodeAttempts = 0; // Reset attempts when code is regenerated
                    course.LastAccessCodeAttempt = null;
                }

                // Update expiration
                course.AccessCodeExpiresAt = request.ExpiresAt;
            }
            else
            {
                // Clear access code if not required
                course.AccessCode = null;
                course.AccessCodeCreatedAt = null;
                course.AccessCodeExpiresAt = null;
                course.AccessCodeAttempts = 0;
                course.LastAccessCodeAttempt = null;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Access code updated for course {CourseId} by lecturer {LecturerId}", 
                request.CourseId, request.LecturerId);

            return new UpdateCourseAccessCodeResponse
            {
                Success = true,
                Message = request.RequiresAccessCode ? "Access code updated successfully" : "Access code requirement disabled",
                AccessCode = course.AccessCode,
                AccessCodeCreatedAt = course.AccessCodeCreatedAt,
                AccessCodeExpiresAt = course.AccessCodeExpiresAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating access code for course {CourseId}", request.CourseId);
            return new UpdateCourseAccessCodeResponse
            {
                Success = false,
                Message = $"Error updating access code: {ex.Message}"
            };
        }
    }
}