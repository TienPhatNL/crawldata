using ClassroomService.Application.Features.Courses.Queries;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Events;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.Courses.Commands;

public class RejectCourseCommandHandler : IRequestHandler<RejectCourseCommand, RejectCourseResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;
    private readonly IAccessCodeService _accessCodeService;
    private readonly ILogger<RejectCourseCommandHandler> _logger;

    public RejectCourseCommandHandler(
        IUnitOfWork unitOfWork,
        IKafkaUserService userService,
        IAccessCodeService accessCodeService,
        ILogger<RejectCourseCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
        _accessCodeService = accessCodeService;
        _logger = logger;
    }

    public async Task<RejectCourseResponse> Handle(RejectCourseCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var course = await _unitOfWork.Courses.GetCourseWithDetailsAsync(request.CourseId, cancellationToken);

            if (course == null)
            {
                return new RejectCourseResponse
                {
                    Success = false,
                    Message = "Course not found"
                };
            }

            if (course.Status != CourseStatus.PendingApproval)
            {
                return new RejectCourseResponse
                {
                    Success = false,
                    Message = $"Course is already {course.Status}. Only pending courses can be rejected."
                };
            }

            var oldStatus = course.Status;
            course.Status = CourseStatus.Rejected;
            course.ApprovedBy = request.RejectedBy;
            course.ApprovedAt = DateTime.UtcNow;
            course.RejectionReason = request.RejectionReason;

            course.AddDomainEvent(new CourseRejectedEvent(
                course.Id,
                request.RejectedBy,
                course.Name,
                course.LecturerId,
                request.RejectionReason));

            course.AddDomainEvent(new CourseStatusChangedEvent(
                course.Id,
                oldStatus,
                CourseStatus.Rejected,
                course.LecturerId,
                request.RejectedBy,
                request.RejectionReason));

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Course {CourseId} rejected by staff {StaffId}", course.Id, request.RejectedBy);

            var lecturer = await _userService.GetUserByIdAsync(course.LecturerId, cancellationToken);
            var lecturerName = lecturer != null && !string.IsNullOrEmpty(lecturer.LastName) && !string.IsNullOrEmpty(lecturer.FirstName)
                ? $"{lecturer.LastName} {lecturer.FirstName}".Trim()
                : lecturer?.FullName ?? "Unknown Lecturer";

            var staff = await _userService.GetUserByIdAsync(request.RejectedBy, cancellationToken);
            var staffName = staff != null && !string.IsNullOrEmpty(staff.LastName) && !string.IsNullOrEmpty(staff.FirstName)
                ? $"{staff.LastName} {staff.FirstName}".Trim()
                : staff?.FullName ?? "Unknown Staff";

            var courseDto = CourseDtoBuilder.BuildCourseDto(
                course: course,
                lecturerName: lecturerName,
                enrollmentCount: 0,
                currentUserId: request.RejectedBy,
                currentUserRole: RoleConstants.Staff,
                accessCodeService: _accessCodeService,
                showFullAccessCodeInfo: false,
                approvedByName: staffName);

            return new RejectCourseResponse
            {
                Success = true,
                Message = "Course rejected successfully",
                Course = courseDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting course {CourseId}", request.CourseId);
            return new RejectCourseResponse
            {
                Success = false,
                Message = $"Error rejecting course: {ex.Message}"
            };
        }
    }
}
