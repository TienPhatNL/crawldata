using ClassroomService.Domain.Constants;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.Enrollments.Queries;

public class GetCourseEnrolledStudentsQueryHandler : IRequestHandler<GetCourseEnrolledStudentsQuery, GetCourseEnrolledStudentsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;
    private readonly ILogger<GetCourseEnrolledStudentsQueryHandler> _logger;

    public GetCourseEnrolledStudentsQueryHandler(
        IUnitOfWork unitOfWork,
        IKafkaUserService userService,
        ILogger<GetCourseEnrolledStudentsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
        _logger = logger;
    }

    public async Task<GetCourseEnrolledStudentsResponse> Handle(GetCourseEnrolledStudentsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate course exists
            var course = await _unitOfWork.Courses.GetByIdAsync(request.CourseId, cancellationToken);

            if (course == null)
            {
                return new GetCourseEnrolledStudentsResponse
                {
                    Success = false,
                    Message = "Course not found",
                    CourseId = request.CourseId
                };
            }

            // Validate authorization - only lecturer of the course or staff/admin can view enrolled students
            var requestingUser = await _userService.GetUserByIdAsync(request.RequestedBy, cancellationToken);
            if (requestingUser == null)
            {
                return new GetCourseEnrolledStudentsResponse
                {
                    Success = false,
                    Message = "User not found",
                    CourseId = request.CourseId,
                    CourseName = course.Name
                };
            }

            bool isAuthorized = requestingUser.Role == RoleConstants.Staff ||
                               requestingUser.Role == RoleConstants.Admin ||
                               (requestingUser.Role == RoleConstants.Lecturer && course.LecturerId == requestingUser.Id) ||
                               requestingUser.Role == RoleConstants.Student; 

            if (!isAuthorized)
            {
                return new GetCourseEnrolledStudentsResponse
                {
                    Success = false,
                    Message = "Unauthorized. Only the course lecturer, staff, or admin can view enrolled students.",
                    CourseId = request.CourseId,
                    CourseName = course.Name
                };
            }

            // Get all enrollments for this course
            var allEnrollments = await _unitOfWork.CourseEnrollments
                .GetEnrollmentsByCourseAsync(request.CourseId, cancellationToken);
            
            var enrollments = allEnrollments
                .Where(e => e.Status == EnrollmentStatus.Active)
                .OrderBy(e => e.JoinedAt)
                .ToList();

            if (!enrollments.Any())
            {
                return new GetCourseEnrolledStudentsResponse
                {
                    Success = true,
                    Message = "No students enrolled in this course",
                    CourseId = request.CourseId,
                    CourseName = course.Name,
                    Students = new List<EnrolledStudentDto>(),
                    TotalStudents = 0
                };
            }

            // Get student IDs
            var studentIds = enrollments.Select(e => e.StudentId).Distinct().ToList();

            // Fetch student information from UserService
            var students = await _userService.GetUsersByIdsAsync(studentIds, cancellationToken);
            var studentDict = students.ToDictionary(s => s.Id);

            // Build enrolled student DTOs
            var enrolledStudents = enrollments
                .Where(e => studentDict.ContainsKey(e.StudentId))
                .Select(e =>
                {
                    var student = studentDict[e.StudentId];
                    return new EnrolledStudentDto
                    {
                        StudentId = student.Id,
                        Email = student.Email,
                        FirstName = student.FirstName,
                        LastName = student.LastName,
                        FullName = student.FullName,
                        StudentIdNumber = student.StudentId,
                        ProfilePictureUrl = student.ProfilePictureUrl,
                        JoinedAt = e.JoinedAt,
                        Status = e.Status.ToString(),
                        EnrollmentId = e.Id
                    };
                })
                .ToList();

            _logger.LogInformation("Retrieved {Count} enrolled students for course {CourseId}", enrolledStudents.Count, request.CourseId);

            return new GetCourseEnrolledStudentsResponse
            {
                Success = true,
                Message = $"Successfully retrieved {enrolledStudents.Count} enrolled students",
                CourseId = request.CourseId,
                CourseName = course.Name,
                Students = enrolledStudents,
                TotalStudents = enrolledStudents.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving enrolled students for course {CourseId}", request.CourseId);
            return new GetCourseEnrolledStudentsResponse
            {
                Success = false,
                Message = $"Error retrieving enrolled students: {ex.Message}",
                CourseId = request.CourseId
            };
        }
    }
}

