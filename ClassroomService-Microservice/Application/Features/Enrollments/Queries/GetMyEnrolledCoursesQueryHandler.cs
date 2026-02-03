using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.Enrollments.Queries;

public class GetMyEnrolledCoursesQueryHandler : IRequestHandler<GetMyEnrolledCoursesQuery, GetMyEnrolledCoursesResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IKafkaUserService _userService;
    private readonly ILogger<GetMyEnrolledCoursesQueryHandler> _logger;

    public GetMyEnrolledCoursesQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IKafkaUserService userService,
        ILogger<GetMyEnrolledCoursesQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _userService = userService;
        _logger = logger;
    }

    public async Task<GetMyEnrolledCoursesResponse> Handle(GetMyEnrolledCoursesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            Guid studentId;
            
            // Determine the student ID to use
            if (request.StudentId != Guid.Empty)
            {
                studentId = request.StudentId;
            }
            else if (_currentUserService.UserId.HasValue)
            {
                studentId = _currentUserService.UserId.Value;
            }
            else
            {
                return new GetMyEnrolledCoursesResponse
                {
                    Success = false,
                    Message = Messages.Error.UserIdNotFound,
                    Courses = new List<EnrolledCourseDto>()
                };
            }

            // Get all active enrollments for the student
            var enrollments = (await _unitOfWork.CourseEnrollments
                .GetEnrollmentsByStudentAsync(studentId, cancellationToken))
                .Where(e => e.Status == EnrollmentStatus.Active)
                .ToList();

            if (!enrollments.Any())
            {
                return new GetMyEnrolledCoursesResponse
                {
                    Success = true,
                    Message = "No enrolled courses found",
                    Courses = new List<EnrolledCourseDto>()
                };
            }

            // Get unique course IDs
            var courseIds = enrollments.Select(e => e.CourseId).Distinct().ToList();

            // Get all courses with details
            var courses = new List<Domain.Entities.Course>();
            foreach (var courseId in courseIds)
            {
                var course = await _unitOfWork.Courses.GetCourseWithDetailsAsync(courseId, cancellationToken);
                if (course != null)
                {
                    courses.Add(course);
                }
            }

            // Get all active enrollments for those courses (for enrollment count)
            var allEnrollments = new List<Domain.Entities.CourseEnrollment>();
            foreach (var courseId in courseIds)
            {
                var courseEnrollments = (await _unitOfWork.CourseEnrollments
                    .GetEnrollmentsByCourseAsync(courseId, cancellationToken))
                    .Where(e => e.Status == EnrollmentStatus.Active);
                allEnrollments.AddRange(courseEnrollments);
            }

            // Get unique lecturer IDs
            var lecturerIds = courses.Select(c => c.LecturerId).Distinct().ToList();
            var lecturers = await _userService.GetUsersByIdsAsync(lecturerIds, cancellationToken);
            var lecturerDict = lecturers.ToDictionary(l => l.Id);

            // Map to DTOs
            var courseDtos = courses.Select(c =>
            {
                var enrollment = enrollments.FirstOrDefault(e => e.CourseId == c.Id);
                var lecturer = lecturerDict.GetValueOrDefault(c.LecturerId);
                var lecturerName = lecturer != null
                    ? $"{lecturer.LastName} {lecturer.FirstName}".Trim()
                    : "Unknown Lecturer";

                var activeEnrollmentCount = allEnrollments.Count(e => e.CourseId == c.Id);

                return new EnrolledCourseDto
                {
                    CourseId = c.Id,
                    CourseCode = c.CourseCode.Code,
                    CourseName = c.Name,
                    Description = c.Description,
                    LecturerName = lecturerName,
                    Term = c.Term.Name,
                    JoinedAt = enrollment?.JoinedAt ?? DateTime.UtcNow,
                    EnrollmentId = enrollment?.Id ?? Guid.Empty,
                    EnrollmentCount = activeEnrollmentCount,
                    Department = c.CourseCode.Department,
                    Img = c.Img
                };
            }).ToList();

            _logger.LogInformation("Retrieved {Count} enrolled courses for student {StudentId}", courseDtos.Count, studentId);

            return new GetMyEnrolledCoursesResponse
            {
                Success = true,
                Message = "Enrolled courses retrieved successfully",
                Courses = courseDtos,
                TotalCount = courseDtos.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving enrolled courses for student");
            return new GetMyEnrolledCoursesResponse
            {
                Success = false,
                Message = $"An error occurred while retrieving enrolled courses: {ex.Message}",
                Courses = new List<EnrolledCourseDto>()
            };
        }
    }
}

