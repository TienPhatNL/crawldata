using MediatR;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Application.Features.Courses.Queries;

public class GetCourseQueryHandler : IRequestHandler<GetCourseQuery, GetCourseResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;
    private readonly IAccessCodeService _accessCodeService;

    public GetCourseQueryHandler(
        IUnitOfWork unitOfWork, 
        IKafkaUserService userService,
        IAccessCodeService accessCodeService)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
        _accessCodeService = accessCodeService;
    }

    public async Task<GetCourseResponse> Handle(GetCourseQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Load course with all required navigation properties
            var course = await _unitOfWork.Courses.GetAsync(
                c => c.Id == request.CourseId, 
                cancellationToken,
                c => c.CourseCode,
                c => c.Term,
                c => c.Enrollments);

            if (course == null)
            {
                return new GetCourseResponse
                {
                    Success = false,
                    Message = "Course not found",
                    Course = null
                };
            }

            // Get lecturer information from UserService
            var lecturer = await _userService.GetUserByIdAsync(course.LecturerId, cancellationToken);
            var lecturerName = lecturer != null 
                ? $"{lecturer.LastName} {lecturer.FirstName}".Trim()
                : "Unknown Lecturer";
            var lecturerImage = lecturer?.ProfilePictureUrl;

            // Check if current user (if student) is enrolled in the course
            bool? isEnrolled = null;
            if (request.CurrentUserId.HasValue && 
                request.CurrentUserRole?.Equals("Student", StringComparison.OrdinalIgnoreCase) == true)
            {
                isEnrolled = course.Enrollments?.Any(e => 
                    e.StudentId == request.CurrentUserId.Value && 
                    e.Status == EnrollmentStatus.Active) ?? false;
            }

            // Build CourseDto with proper access control
            var courseDto = CourseDtoBuilder.BuildCourseDto(
                course: course,
                lecturerName: lecturerName,
                enrollmentCount: course.Enrollments?.Count(e => e.Status == EnrollmentStatus.Active) ?? 0,
                currentUserId: request.CurrentUserId,
                currentUserRole: request.CurrentUserRole,
                accessCodeService: _accessCodeService,
                showFullAccessCodeInfo: false, // Never show full access code info in individual course view
                lecturerImage: lecturerImage
            );

            return new GetCourseResponse
            {
                Success = true,
                Message = "Course retrieved successfully",
                Course = courseDto,
                IsEnrolled = isEnrolled
            };
        }
        catch (Exception ex)
        {
            return new GetCourseResponse
            {
                Success = false,
                Message = $"Error retrieving course: {ex.Message}",
                Course = null
            };
        }
    }
}