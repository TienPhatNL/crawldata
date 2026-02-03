using MediatR;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Application.Features.Enrollments.Commands;

public class UnenrollStudentCommandHandler : IRequestHandler<UnenrollStudentCommand, UnenrollStudentResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;

    public UnenrollStudentCommandHandler(IUnitOfWork unitOfWork, IKafkaUserService userService)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
    }

    public async Task<UnenrollStudentResponse> Handle(UnenrollStudentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Find the active enrollment
            var enrollment = await _unitOfWork.CourseEnrollments.GetEnrollmentAsync(
                request.CourseId,
                request.StudentId,
                cancellationToken);

            if (enrollment == null || enrollment.Status != EnrollmentStatus.Active)
            {
                return new UnenrollStudentResponse
                {
                    Success = false,
                    Message = "Active enrollment not found. The student may not be enrolled in this course.",
                    UnenrolledStudent = null
                };
            }

            // Get course information
            var course = await _unitOfWork.Courses.GetCourseWithDetailsAsync(request.CourseId, cancellationToken);
            if (course == null)
            {
                return new UnenrollStudentResponse
                {
                    Success = false,
                    Message = "Course not found.",
                    UnenrolledStudent = null
                };
            }

            // Get student information from UserService
            var student = await _userService.GetUserByIdAsync(request.StudentId, cancellationToken);
            var studentName = student != null 
                ? $"{student.LastName} {student.FirstName}".Trim()
                : "Unknown Student";

            // Update the enrollment status instead of deleting
            enrollment.Status = EnrollmentStatus.Withdrawn;
            enrollment.UnenrolledAt = DateTime.UtcNow;
            enrollment.UnenrollmentReason = request.Reason ?? "Unenrolled by administrator";
            enrollment.UnenrolledBy = request.UnenrolledBy;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Create the enrollment DTO with updated information
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

            return new UnenrollStudentResponse
            {
                Success = true,
                Message = $"Student {studentName} has been successfully unenrolled from {course.Name}",
                UnenrolledStudent = enrollmentDto
            };
        }
        catch (Exception ex)
        {
            return new UnenrollStudentResponse
            {
                Success = false,
                Message = $"Error unenrolling student: {ex.Message}",
                UnenrolledStudent = null
            };
        }
    }
}