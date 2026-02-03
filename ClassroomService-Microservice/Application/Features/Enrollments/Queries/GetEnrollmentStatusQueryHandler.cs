using MediatR;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Application.Features.Enrollments.Queries;

public class GetEnrollmentStatusQueryHandler : IRequestHandler<GetEnrollmentStatusQuery, EnrollmentStatusResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;

    public GetEnrollmentStatusQueryHandler(IUnitOfWork unitOfWork, IKafkaUserService userService)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
    }

    public async Task<EnrollmentStatusResponse> Handle(GetEnrollmentStatusQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Get course information
            var course = await _unitOfWork.Courses.GetCourseWithDetailsAsync(request.CourseId, cancellationToken);

            if (course == null)
            {
                return new EnrollmentStatusResponse
                {
                    Success = false,
                    Message = "Course not found",
                    IsEnrolled = false
                };
            }

            // Check enrollment status
            var enrollment = await _unitOfWork.CourseEnrollments
                .GetAsync(e => e.CourseId == request.CourseId 
                    && e.StudentId == request.StudentId, cancellationToken);

            // Get lecturer information
            var lecturer = await _userService.GetUserByIdAsync(course.LecturerId, cancellationToken);
            var lecturerName = lecturer != null 
                ? $"{lecturer.LastName} {lecturer.FirstName}".Trim()
                : "Unknown Lecturer";

            var courseInfo = new CourseInfo
            {
                Id = course.Id,
                CourseCode = course.CourseCode.Code,
                Name = course.Name,
                LecturerName = lecturerName
            };

            if (enrollment == null)
            {
                return new EnrollmentStatusResponse
                {
                    Success = true,
                    Message = "Student is not enrolled in this course",
                    IsEnrolled = false,
                    Course = courseInfo
                };
            }

            var isActivelyEnrolled = enrollment.Status == EnrollmentStatus.Active;

            return new EnrollmentStatusResponse
            {
                Success = true,
                Message = isActivelyEnrolled ? "Student is enrolled in this course" : $"Student enrollment status: {enrollment.Status}",
                IsEnrolled = isActivelyEnrolled,
                Status = enrollment.Status,
                JoinedAt = enrollment.JoinedAt,
                Course = courseInfo
            };
        }
        catch (Exception ex)
        {
            return new EnrollmentStatusResponse
            {
                Success = false,
                Message = $"Error checking enrollment status: {ex.Message}",
                IsEnrolled = false
            };
        }
    }
}