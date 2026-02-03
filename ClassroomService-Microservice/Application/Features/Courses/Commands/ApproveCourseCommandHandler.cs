using MediatR;
using Microsoft.Extensions.Logging;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Events;
using ClassroomService.Application.Features.Courses.Queries;
using ClassroomService.Domain.Constants;

namespace ClassroomService.Application.Features.Courses.Commands;

public class ApproveCourseCommandHandler : IRequestHandler<ApproveCourseCommand, ApproveCourseResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;
    private readonly IAccessCodeService _accessCodeService;
    private readonly ILogger<ApproveCourseCommandHandler> _logger;

    public ApproveCourseCommandHandler(
        IUnitOfWork unitOfWork,
        IKafkaUserService userService,
        IAccessCodeService accessCodeService,
        ILogger<ApproveCourseCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
        _accessCodeService = accessCodeService;
        _logger = logger;
    }

    public async Task<ApproveCourseResponse> Handle(ApproveCourseCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var course = await _unitOfWork.Courses.GetCourseWithDetailsAsync(request.CourseId, cancellationToken);

            if (course == null)
            {
                return new ApproveCourseResponse
                {
                    Success = false,
                    Message = "Course not found"
                };
            }

            if (course.Status != CourseStatus.PendingApproval)
            {
                return new ApproveCourseResponse
                {
                    Success = false,
                    Message = $"Course is already {course.Status}. Only pending courses can be approved."
                };
            }

            var oldStatus = course.Status;
            course.Status = CourseStatus.Active;
            course.ApprovedBy = request.ApprovedBy;
            course.ApprovedAt = DateTime.UtcNow;
            course.ApprovalComments = request.Comments;

            course.AddDomainEvent(new CourseApprovedEvent(
                course.Id,
                request.ApprovedBy,
                course.Name,
                course.LecturerId,
                request.Comments));

            course.AddDomainEvent(new CourseStatusChangedEvent(
                course.Id,
                oldStatus,
                CourseStatus.Active,
                course.LecturerId,
                request.ApprovedBy,
                request.Comments));

            await _unitOfWork.Courses.UpdateAsync(course, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Course {CourseId} approved by staff {StaffId}", course.Id, request.ApprovedBy);

            var lecturer = await _userService.GetUserByIdAsync(course.LecturerId, cancellationToken);
            var lecturerName = lecturer != null && !string.IsNullOrEmpty(lecturer.LastName) && !string.IsNullOrEmpty(lecturer.FirstName)
                ? $"{lecturer.LastName} {lecturer.FirstName}".Trim()
                : lecturer?.FullName ?? "Unknown Lecturer";

            var staff = await _userService.GetUserByIdAsync(request.ApprovedBy, cancellationToken);
            var staffName = staff != null && !string.IsNullOrEmpty(staff.LastName) && !string.IsNullOrEmpty(staff.FirstName)
                ? $"{staff.LastName} {staff.FirstName}".Trim()
                : staff?.FullName ?? "Unknown Staff";

            var courseDto = CourseDtoBuilder.BuildCourseDto(
                course: course,
                lecturerName: lecturerName,
                enrollmentCount: 0,
                currentUserId: request.ApprovedBy,
                currentUserRole: RoleConstants.Staff,
                accessCodeService: _accessCodeService,
                showFullAccessCodeInfo: false,
                approvedByName: staffName);

            return new ApproveCourseResponse
            {
                Success = true,
                Message = "Course approved successfully",
                Course = courseDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving course {CourseId}", request.CourseId);
            return new ApproveCourseResponse
            {
                Success = false,
                Message = $"Error approving course: {ex.Message}"
            };
        }
    }
}
