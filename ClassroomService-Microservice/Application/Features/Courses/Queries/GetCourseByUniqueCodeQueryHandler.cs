using ClassroomService.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClassroomService.Application.Features.Courses.Queries;

public class GetCourseByUniqueCodeQueryHandler : IRequestHandler<GetCourseByUniqueCodeQuery, GetCourseResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;
    private readonly IAccessCodeService _accessCodeService;

    public GetCourseByUniqueCodeQueryHandler(
        IUnitOfWork unitOfWork,
        IKafkaUserService userService,
        IAccessCodeService accessCodeService)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
        _accessCodeService = accessCodeService;
    }

    public async Task<GetCourseResponse> Handle(GetCourseByUniqueCodeQuery request, CancellationToken cancellationToken)
    {
        // Get course by unique code with related data
        var course = await _unitOfWork.Courses
            .GetCourseByUniqueCodeAsync(request.UniqueCode, cancellationToken);

        if (course == null)
        {
            return new GetCourseResponse
            {
                Success = false,
                Message = $"Course with unique code '{request.UniqueCode}' not found",
                Course = null
            };
        }

        // Get lecturer name
        var lecturer = await _userService.GetUserByIdAsync(course.LecturerId, cancellationToken);
        var lecturerName = lecturer != null
            ? $"{lecturer.LastName} {lecturer.FirstName}".Trim()
            : "Unknown Lecturer";
        var lecturerImage = lecturer?.ProfilePictureUrl;

        // Get enrollment count
        var enrollmentCount = await _unitOfWork.CourseEnrollments
            .GetActiveEnrollmentCountAsync(course.Id, cancellationToken);

        // Check if current user (if student) is enrolled in the course
        bool? isEnrolled = null;
        if (request.CurrentUserId.HasValue && 
            request.CurrentUserRole?.Equals("Student", StringComparison.OrdinalIgnoreCase) == true)
        {
            isEnrolled = await _unitOfWork.CourseEnrollments
                .ExistsAsync(e => 
                    e.CourseId == course.Id && 
                    e.StudentId == request.CurrentUserId.Value && 
                    e.Status == Domain.Enums.EnrollmentStatus.Active, 
                    cancellationToken);
        }

        // Get approver name if approved
        string? approvedByName = null;
        if (course.ApprovedBy.HasValue)
        {
            var approver = await _userService.GetUserByIdAsync(course.ApprovedBy.Value, cancellationToken);
            approvedByName = approver != null
                ? $"{approver.LastName} {approver.FirstName}".Trim()
                : "Unknown Staff";
        }

        // Build CourseDto with proper access control
        var courseDto = CourseDtoBuilder.BuildCourseDto(
            course,
            lecturerName,
            enrollmentCount,
            request.CurrentUserId,
            request.CurrentUserRole,
            _accessCodeService,
            showFullAccessCodeInfo: false,
            approvedByName: approvedByName,
            lecturerImage: lecturerImage);

        return new GetCourseResponse
        {
            Success = true,
            Message = "Course retrieved successfully",
            Course = courseDto,
            IsEnrolled = isEnrolled
        };
    }
}
