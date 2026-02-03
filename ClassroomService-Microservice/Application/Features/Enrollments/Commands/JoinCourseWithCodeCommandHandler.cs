using MediatR;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Events;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Constants; // <-- để dùng RoleConstants
using Microsoft.Extensions.Logging;

namespace ClassroomService.Application.Features.Enrollments.Commands;

public class JoinCourseWithCodeCommandHandler : IRequestHandler<JoinCourseWithCodeCommand, EnrollmentResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;
    private readonly IAccessCodeService _accessCodeService;
    private readonly ILogger<JoinCourseWithCodeCommandHandler> _logger;

    public JoinCourseWithCodeCommandHandler(
        IUnitOfWork unitOfWork,
        IKafkaUserService userService,
        IAccessCodeService accessCodeService,
        ILogger<JoinCourseWithCodeCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
        _accessCodeService = accessCodeService;
        _logger = logger;
    }

    public async Task<EnrollmentResponse> Handle(JoinCourseWithCodeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "JoinCourseWithCodeCommand started. CourseId={CourseId}, StudentId={StudentId}",
                request.CourseId, request.StudentId);

            // Get course with access code requirements
            var course = await _unitOfWork.Courses.GetCourseWithDetailsAsync(request.CourseId, cancellationToken);

            if (course == null || course.Status != CourseStatus.Active)
            {
                return new EnrollmentResponse
                {
                    Success = false,
                    Message = Messages.Error.CourseNotAvailableForEnrollment
                };
            }

            // Validate that the student exists and has the correct role
            // ❗ Dùng RoleConstants.Student thay vì hard-code "Student"
            var isValidStudent = await _userService.ValidateUserAsync(
                request.StudentId,
                RoleConstants.Student,
                cancellationToken);

            if (!isValidStudent)
            {
                _logger.LogWarning(
                    "Invalid student or role when joining course. CourseId={CourseId}, StudentId={StudentId}, RequiredRole={Role}",
                    request.CourseId, request.StudentId, RoleConstants.Student);

                return new EnrollmentResponse
                {
                    Success = false,
                    Message = Messages.Error.StudentNotFound // "Invalid student. User must exist and have Student role."
                };
            }

            // Check for any existing enrollment
            var existingEnrollment = await _unitOfWork.CourseEnrollments
                .GetAsync(e => e.CourseId == request.CourseId
                    && e.StudentId == request.StudentId, cancellationToken);

            // If already actively enrolled, return error
            if (existingEnrollment != null && existingEnrollment.Status == EnrollmentStatus.Active)
            {
                return new EnrollmentResponse
                {
                    Success = false,
                    Message = Messages.Error.AlreadyEnrolled
                };
            }

            // SECURITY: If course requires access code, it MUST be validated
            if (course.RequiresAccessCode)
            {
                // Always require the access code when course needs it
                if (string.IsNullOrWhiteSpace(request.AccessCode))
                {
                    _logger.LogWarning(
                        "Access code required but not provided for course {CourseId} by student {StudentId}",
                        request.CourseId, request.StudentId);

                    return new EnrollmentResponse
                    {
                        Success = false,
                        Message = Messages.Error.AccessCodeRequired
                    };
                }

                // Check rate limiting before validating code
                if (_accessCodeService.IsRateLimited(course))
                {
                    _logger.LogWarning(
                        "Rate limit exceeded for course {CourseId} by student {StudentId}",
                        request.CourseId, request.StudentId);

                    return new EnrollmentResponse
                    {
                        Success = false,
                        Message = Messages.Error.RateLimitExceeded
                    };
                }

                // Validate the provided access code
                if (!_accessCodeService.ValidateAccessCode(request.AccessCode, course))
                {
                    _accessCodeService.RecordFailedAttempt(course);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    _logger.LogWarning(
                        "Invalid access code attempt for course {CourseId} by student {StudentId}",
                        request.CourseId, request.StudentId);

                    return new EnrollmentResponse
                    {
                        Success = false,
                        Message = Messages.Error.InvalidAccessCode
                    };
                }

                _logger.LogInformation(
                    "Valid access code provided for course {CourseId} by student {StudentId}",
                    request.CourseId, request.StudentId);
            }

            CourseEnrollment enrollment;
            bool isReactivation = false;

            if (existingEnrollment != null)
            {
                // Reactivate existing enrollment
                enrollment = existingEnrollment;
                enrollment.Status = EnrollmentStatus.Active;
                enrollment.JoinedAt = DateTime.UtcNow;
                enrollment.UnenrolledAt = null;
                enrollment.UnenrollmentReason = null;
                enrollment.UnenrolledBy = null;
                isReactivation = true;

                _logger.LogInformation(
                    "Reactivating enrollment for student {StudentId} in course {CourseId}",
                    request.StudentId, request.CourseId);
            }
            else
            {
                // Create new enrollment
                enrollment = new CourseEnrollment
                {
                    Id = Guid.NewGuid(),
                    CourseId = request.CourseId,
                    StudentId = request.StudentId,
                    JoinedAt = DateTime.UtcNow,
                    Status = EnrollmentStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.CourseEnrollments.AddAsync(enrollment, cancellationToken);

                _logger.LogInformation(
                    "Creating new enrollment for student {StudentId} in course {CourseId}",
                    request.StudentId, request.CourseId);
            }

            enrollment.AddDomainEvent(new StudentEnrolledEvent(
                enrollment.Id,
                enrollment.CourseId,
                enrollment.StudentId,
                enrollment.JoinedAt,
                course.LecturerId,
                course.Name));

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Get student information for response
            var student = await _userService.GetUserByIdAsync(request.StudentId, cancellationToken);
            var studentName = student != null
                ? $"{student.LastName} {student.FirstName}".Trim()
                : "Unknown Student";

            var enrollmentDto = new EnrollmentDto
            {
                Id = enrollment.Id,
                CourseId = enrollment.CourseId,
                CourseName = course.Name,
                CourseCode = course.CourseCode.Code,
                StudentId = enrollment.StudentId,
                StudentName = studentName,
                JoinedAt = enrollment.JoinedAt,
                UnenrolledAt = enrollment.UnenrolledAt,
                Status = enrollment.Status,
                UnenrollmentReason = enrollment.UnenrollmentReason,
                CreatedAt = enrollment.CreatedAt
            };

            var message = isReactivation
                ? $"Successfully rejoined {course.Name}!"
                : $"Successfully joined {course.Name}!";

            _logger.LogInformation(
                "Student {StudentId} successfully {Action} course {CourseId}",
                request.StudentId, isReactivation ? "rejoined" : "joined", request.CourseId);

            return new EnrollmentResponse
            {
                Success = true,
                Message = message,
                EnrollmentId = enrollment.Id,
                Enrollment = enrollmentDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error joining course {CourseId} for student {StudentId}",
                request.CourseId, request.StudentId);

            return new EnrollmentResponse
            {
                Success = false,
                Message = Messages.Helpers.FormatError(
                    Messages.Error.JoinCourseFailed,
                    ex.Message)
            };
        }
    }
}
