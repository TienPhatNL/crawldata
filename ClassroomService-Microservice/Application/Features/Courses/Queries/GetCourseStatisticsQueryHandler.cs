using ClassroomService.Domain.Constants;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.Courses.Queries;

public class GetCourseStatisticsQueryHandler : IRequestHandler<GetCourseStatisticsQuery, GetCourseStatisticsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;
    private readonly IAccessCodeService _accessCodeService;
    private readonly ILogger<GetCourseStatisticsQueryHandler> _logger;

    public GetCourseStatisticsQueryHandler(
        IUnitOfWork unitOfWork,
        IKafkaUserService userService,
        IAccessCodeService accessCodeService,
        ILogger<GetCourseStatisticsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
        _accessCodeService = accessCodeService;
        _logger = logger;
    }

    public async Task<GetCourseStatisticsResponse> Handle(GetCourseStatisticsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving statistics for course {CourseId}", request.CourseId);

            // Get course with all related data
            var course = await _unitOfWork.Courses.GetCourseWithDetailsAsync(request.CourseId, cancellationToken);

            if (course == null)
            {
                _logger.LogWarning("Course not found: {CourseId}", request.CourseId);
                return new GetCourseStatisticsResponse
                {
                    Success = false,
                    Message = Messages.Error.CourseNotFound
                };
            }

            // Get lecturer information
            var lecturer = await _userService.GetUserByIdAsync(course.LecturerId, cancellationToken);
            var lecturerName = lecturer != null 
                ? $"{lecturer.LastName} {lecturer.FirstName}".Trim()
                : "Unknown Lecturer";

            // Build course DTO
            var courseDto = CourseDtoBuilder.BuildCourseDto(
                course: course,
                lecturerName: lecturerName,
                enrollmentCount: course.Enrollments?.Count(e => e.Status == EnrollmentStatus.Active) ?? 0,
                currentUserId: null,
                currentUserRole: null,
                accessCodeService: _accessCodeService,
                showFullAccessCodeInfo: false
            );

            // Calculate statistics (null-safe)
            var totalEnrollments = course.Enrollments?.Count(e => e.Status == EnrollmentStatus.Active) ?? 0;
            var totalAssignments = course.Assignments?.Count ?? 0;
            var totalGroups = course.Groups?.Count ?? 0;

            // Get recent enrollments (last 7 days)
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
            var recentEnrollments = course.Enrollments?.Count(e => 
                e.JoinedAt >= sevenDaysAgo && e.Status == EnrollmentStatus.Active) ?? 0;

            // Get last activity
            var lastActivity = course.UpdatedAt ?? course.CreatedAt;
            if (course.Enrollments != null && course.Enrollments.Any())
            {
                var lastEnrollmentDate = course.Enrollments.Max(e => e.JoinedAt);
                if (lastEnrollmentDate > lastActivity)
                    lastActivity = lastEnrollmentDate;
            }

            // Calculate enrollments by month (for charts) - last 6 months
            var enrollmentsByMonth = new Dictionary<string, int>();
            if (course.Enrollments != null && course.Enrollments.Any())
            {
                var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
                var enrollmentsInRange = course.Enrollments
                    .Where(e => e.JoinedAt >= sixMonthsAgo && e.Status == EnrollmentStatus.Active)
                    .GroupBy(e => e.JoinedAt.ToString("yyyy-MM"))
                    .OrderBy(g => g.Key)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Fill in missing months with 0
                for (int i = 5; i >= 0; i--)
                {
                    var monthKey = DateTime.UtcNow.AddMonths(-i).ToString("yyyy-MM");
                    enrollmentsByMonth[monthKey] = enrollmentsInRange.GetValueOrDefault(monthKey, 0);
                }
            }

            // Note: TotalChatMessages would need to be queried from chat repository
            // For now, we'll set it to 0 as a placeholder
            var totalChatMessages = 0;

            // Try to get actual counts if repositories are available
            try
            {
                // Get chat messages count (if Chat entity exists)
                var chats = await _unitOfWork.Chats.GetManyAsync(
                    c => c.CourseId == request.CourseId,
                    cancellationToken);
                totalChatMessages = chats?.Count() ?? 0;
            }
            catch
            {
                // Chats might not be implemented yet
                totalChatMessages = 0;
            }

            var statistics = new CourseStatisticsDto
            {
                Course = courseDto,
                TotalEnrollments = totalEnrollments,
                TotalAssignments = totalAssignments,
                TotalGroups = totalGroups,
                TotalChatMessages = totalChatMessages,
                RecentEnrollments = recentEnrollments,
                LastActivity = lastActivity,
                EnrollmentsByMonth = enrollmentsByMonth
            };

            _logger.LogInformation("Successfully retrieved statistics for course {CourseId}", request.CourseId);

            return new GetCourseStatisticsResponse
            {
                Success = true,
                Message = Messages.Success.StatisticsRetrieved,
                Statistics = statistics
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving statistics for course {CourseId}: {ErrorMessage}",
                request.CourseId, ex.Message);

            return new GetCourseStatisticsResponse
            {
                Success = false,
                Message = $"Error retrieving course statistics: {ex.Message}"
            };
        }
    }
}