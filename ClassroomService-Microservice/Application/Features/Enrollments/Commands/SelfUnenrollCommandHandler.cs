using MediatR;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.DTOs;

namespace ClassroomService.Application.Features.Enrollments.Commands;

public class SelfUnenrollCommandHandler : IRequestHandler<SelfUnenrollCommand, UnenrollStudentResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;
    private readonly ILogger<SelfUnenrollCommandHandler> _logger;

    public SelfUnenrollCommandHandler(
        IUnitOfWork unitOfWork,
        IKafkaUserService userService,
        ILogger<SelfUnenrollCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
        _logger = logger;
    }

    public async Task<UnenrollStudentResponse> Handle(SelfUnenrollCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(Messages.Logging.StudentUnenrolling, request.StudentId, request.CourseId);

            // Get the course with navigation properties
            var course = await _unitOfWork.Courses.GetAsync(
                c => c.Id == request.CourseId,
                cancellationToken,
                c => c.CourseCode,
                c => c.Term);

            if (course == null)
            {
                return new UnenrollStudentResponse
                {
                    Success = false,
                    Message = Messages.Error.CourseNotFound,
                    UnenrolledStudent = null
                };
            }

            // Find the active enrollment
            var enrollment = await _unitOfWork.CourseEnrollments.GetEnrollmentAsync(
                request.CourseId,
                request.StudentId,
                cancellationToken);

            if (enrollment == null)
            {
                return new UnenrollStudentResponse
                {
                    Success = false,
                    Message = Messages.Error.NotEnrolled,
                    UnenrolledStudent = null
                };
            }

            // Check if already unenrolled
            if (enrollment.Status != EnrollmentStatus.Active)
            {
                return new UnenrollStudentResponse
                {
                    Success = false,
                    Message = $"You are not actively enrolled in this course. Current status: {enrollment.Status}",
                    UnenrolledStudent = null
                };
            }

            // Get student information
            var student = await _userService.GetUserByIdAsync(request.StudentId, cancellationToken);
            var studentName = student != null 
                ? $"{student.LastName} {student.FirstName}".Trim()
                : "Unknown Student";

            // Update the enrollment status
            enrollment.Status = EnrollmentStatus.Withdrawn;
            enrollment.UnenrolledAt = DateTime.UtcNow;
            enrollment.UnenrollmentReason = "Self-unenrollment";
            enrollment.UnenrolledBy = request.StudentId; // Self-unenrolled

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(Messages.Logging.StudentUnenrolled, request.StudentId, request.CourseId);

            // Create enrollment DTO for response
            var enrollmentDto = new EnrollmentDto
            {
                Id = enrollment.Id,
                CourseId = enrollment.CourseId,
                CourseName = course.Name,
                CourseCode = course.CourseCode?.Code ?? "N/A",
                StudentId = enrollment.StudentId,
                StudentName = studentName,
                Status = enrollment.Status,
                JoinedAt = enrollment.JoinedAt,
                UnenrolledAt = enrollment.UnenrolledAt,
                UnenrollmentReason = enrollment.UnenrollmentReason ?? string.Empty,
                CreatedAt = enrollment.CreatedAt
            };

            return new UnenrollStudentResponse
            {
                Success = true,
                Message = Messages.Success.SelfUnenrolled,
                UnenrolledStudent = enrollmentDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unenrolling student {StudentId} from course {CourseId}: {ErrorMessage}",
                request.StudentId, request.CourseId, ex.Message);

            return new UnenrollStudentResponse
            {
                Success = false,
                Message = Messages.Helpers.FormatError(Messages.Error.SelfUnenrollmentFailed, ex.Message),
                UnenrolledStudent = null
            };
        }
    }
}
