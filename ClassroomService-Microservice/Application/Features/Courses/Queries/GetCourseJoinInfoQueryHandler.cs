using MediatR;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace ClassroomService.Application.Features.Courses.Queries;

public class GetCourseJoinInfoQueryHandler : IRequestHandler<GetCourseJoinInfoQuery, GetCourseJoinInfoResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAccessCodeService _accessCodeService;

    public GetCourseJoinInfoQueryHandler(
        IUnitOfWork unitOfWork,
        IKafkaUserService userService,
        IHttpContextAccessor httpContextAccessor,
        IAccessCodeService accessCodeService)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
        _httpContextAccessor = httpContextAccessor;
        _accessCodeService = accessCodeService;
    }

    public async Task<GetCourseJoinInfoResponse> Handle(GetCourseJoinInfoQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Load course with required navigation properties
            var course = await _unitOfWork.Courses.GetAsync(
                c => c.Id == request.CourseId,
                cancellationToken,
                c => c.CourseCode,
                c => c.Term,
                c => c.Enrollments);

            if (course == null)
            {
                return new GetCourseJoinInfoResponse
                {
                    Success = false,
                    Message = "Course not found",
                    Course = null,
                    IsEnrolled = null
                };
            }

            if (course.CourseCode == null)
            {
                return new GetCourseJoinInfoResponse
                {
                    Success = false,
                    Message = "Course data is incomplete (missing course code)",
                    Course = null,
                    IsEnrolled = null
                };
            }

            // Only allow join if course is Active
            if (course.Status != CourseStatus.Active)
            {
                return new GetCourseJoinInfoResponse
                {
                    Success = false,
                    Message = $"This course is not currently available for enrollment. Status: {course.Status}",
                    Course = null,
                    IsEnrolled = null
                };
            }

            // Get lecturer information from UserService
            var lecturer = await _userService.GetUserByIdAsync(course.LecturerId, cancellationToken);
            var lecturerName = lecturer != null
                ? $"{lecturer.LastName} {lecturer.FirstName}".Trim()
                : "Unknown Lecturer";
            var lecturerImage = lecturer?.ProfilePictureUrl;

            var activeEnrollmentCount = course.Enrollments?.Count(e => e.Status == EnrollmentStatus.Active) ?? 0;

            // Build CourseDto with proper access control (same builder as GetCourseQueryHandler)
            var courseDto = CourseDtoBuilder.BuildCourseDto(
                course: course,
                lecturerName: lecturerName,
                enrollmentCount: activeEnrollmentCount,
                currentUserId: request.UserId,          // join-info only has UserId
                currentUserRole: null,                  // no role info here, can extend query later if needed
                accessCodeService: _accessCodeService,
                showFullAccessCodeInfo: false,          // do not expose full access code info
                lecturerImage: lecturerImage
            );

            // Determine enrollment status of current user (if provided)
            bool? isEnrolled = null;
            if (request.UserId.HasValue)
            {
                isEnrolled = course.Enrollments?.Any(e =>
                    e.StudentId == request.UserId.Value &&
                    e.Status == EnrollmentStatus.Active) ?? false;
            }

            return new GetCourseJoinInfoResponse
            {
                Success = true,
                Message = "Course information retrieved successfully",
                Course = courseDto,
                IsEnrolled = isEnrolled
            };
        }
        catch (Exception ex)
        {
            return new GetCourseJoinInfoResponse
            {
                Success = false,
                Message = $"Error retrieving course information: {ex.Message}",
                Course = null,
                IsEnrolled = null
            };
        }
    }

    private string BuildJoinUrl(Guid courseId)
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        if (request == null)
        {
            return $"/courses/{courseId}/join";
        }

        var scheme = request.Scheme;
        var host = request.Host.Value;
        return $"{scheme}://{host}/api/Courses/{courseId}/join";
    }
}
