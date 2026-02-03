using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Application.Features.Dashboard.Helpers;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.Dashboard.Queries;

// Handler 1: Get Courses by Term
public class GetCoursesQueryHandler 
    : IRequestHandler<GetCoursesQuery, DashboardResponse<CurrentCoursesDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IKafkaUserService _userService;

    public GetCoursesQueryHandler(
        IUnitOfWork unitOfWork, 
        ICurrentUserService currentUserService,
        IKafkaUserService userService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _userService = userService;
    }

    public async Task<DashboardResponse<CurrentCoursesDto>> Handle(
        GetCoursesQuery request, 
        CancellationToken cancellationToken)
    {
        var studentId = _currentUserService.UserId!.Value;

        // Get the specified term
        var term = await _unitOfWork.Terms.GetByIdAsync(request.TermId, cancellationToken);

        if (term == null)
        {
            return new DashboardResponse<CurrentCoursesDto>
            {
                Success = false,
                Message = "Term not found",
                Data = null
            };
        }

        // Get enrollments
        var enrollments = await _unitOfWork.CourseEnrollments
            .GetManyAsync(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Active, cancellationToken);

        var courses = new List<CurrentCourseDto>();

        foreach (var enrollment in enrollments)
        {
            var course = await _unitOfWork.Courses.GetByIdAsync(enrollment.CourseId, cancellationToken);
            if (course == null || course.TermId != term.Id) continue;

            var courseCode = await _unitOfWork.CourseCodes.GetByIdAsync(course.CourseCodeId, cancellationToken);
            var lecturer = await _userService.GetUserByIdAsync(course.LecturerId, cancellationToken);

            // Get assignments
            var assignments = await _unitOfWork.Assignments
                .GetManyAsync(a => a.CourseId == course.Id, cancellationToken);

            // Get reports (includes both individual and group submissions)
            var reports = await StudentReportHelper.GetStudentReportsAsync(
                _unitOfWork,
                studentId,
                assignments.Select(a => a.Id).ToList(),
                cancellationToken);

            var completedAssignments = reports.Count(r => r.Status == ReportStatus.Graded);
            var pendingAssignments = assignments.Count() - reports.Count(r => r.Status != ReportStatus.Draft);
            var totalAssignments = assignments.Count();

            // Calculate current grade
            decimal? courseGrade = null;
            if (reports.Any(r => r.Grade.HasValue))
            {
                var totalPercentage = 0m;
                var gradeCount = 0;

                foreach (var assignment in assignments)
                {
                    var report = await StudentReportHelper.GetStudentReportForAssignmentAsync(
                        _unitOfWork,
                        studentId,
                        assignment.Id,
                        cancellationToken);
                    if (report == null || !report.Grade.HasValue) continue;

                    var percentage = (report.Grade!.Value / assignment.MaxPoints.Value) * 100;
                    totalPercentage += percentage;
                    gradeCount++;
                }

                courseGrade = gradeCount > 0 ? totalPercentage / gradeCount : null;
            }

            courses.Add(new CurrentCourseDto
            {
                CourseId = course.Id,
                CourseName = course.Name,
                CourseCode = courseCode?.Code ?? "N/A",
                LecturerName = lecturer?.FullName ?? "Unknown",
                PendingAssignments = pendingAssignments,
                CompletedAssignments = completedAssignments,
                TotalAssignments = totalAssignments,
                CurrentGrade = courseGrade,
                ProgressPercentage = totalAssignments > 0 ? (decimal)completedAssignments / totalAssignments * 100 : 0,
                CourseImage = course.Img
            });
        }

        return new DashboardResponse<CurrentCoursesDto>
        {
            Success = true,
            Data = new CurrentCoursesDto
            {
                Courses = courses,
                TotalEnrolled = courses.Count,
                CurrentTermName = term.Name
            }
        };
    }
}

// Handler 2: Get Student Performance Analytics
public class GetStudentPerformanceAnalyticsQueryHandler 
    : IRequestHandler<GetStudentPerformanceAnalyticsQuery, DashboardResponse<StudentPerformanceAnalyticsDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetStudentPerformanceAnalyticsQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<DashboardResponse<StudentPerformanceAnalyticsDto>> Handle(
        GetStudentPerformanceAnalyticsQuery request, 
        CancellationToken cancellationToken)
    {
        var studentId = _currentUserService.UserId!.Value;

        // Get enrollments
        var enrollments = await _unitOfWork.CourseEnrollments
            .GetManyAsync(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Active, cancellationToken);

        // Filter by term if needed
        if (request.TermId.HasValue)
        {
            var termCourses = await _unitOfWork.Courses
                .GetManyAsync(c => c.TermId == request.TermId.Value, cancellationToken);
            var termCourseIds = termCourses.Select(c => c.Id).ToHashSet();
            enrollments = enrollments.Where(e => termCourseIds.Contains(e.CourseId)).ToList();
        }

        var courseIds = enrollments.Select(e => e.CourseId).ToList();

        // Get all assignments
        var allAssignments = await _unitOfWork.Assignments
            .GetManyAsync(a => courseIds.Contains(a.CourseId), cancellationToken);

        // Get all reports (includes both individual and group submissions)
        var allReports = await StudentReportHelper.GetStudentReportsAsync(
            _unitOfWork,
            studentId,
            allAssignments.Select(a => a.Id).ToList(),
            cancellationToken);
        allReports = allReports.Where(r => r.Status != ReportStatus.Draft).ToList();

        // Calculate submission statistics
        var onTimeSubmissions = allReports.Count(r => r.SubmittedAt <= allAssignments
            .FirstOrDefault(a => a.Id == r.AssignmentId)?.DueDate);
        var lateSubmissions = allReports.Count() - onTimeSubmissions;

        // Calculate average grade
        var gradedReports = allReports.Where(r => r.Grade.HasValue).ToList();
        var averageGrade = gradedReports.Any() ? gradedReports.Average(r => r.Grade!.Value) : 0;

        // Course performance
        var coursePerformance = new List<CoursePerformanceDto>();
        foreach (var enrollment in enrollments)
        {
            var course = await _unitOfWork.Courses.GetByIdAsync(enrollment.CourseId, cancellationToken);
            var courseAssignments = allAssignments.Where(a => a.CourseId == enrollment.CourseId).ToList();
            var courseReports = allReports.Where(r => courseAssignments.Select(a => a.Id).Contains(r.AssignmentId)).ToList();

            var avgGrade = courseReports.Where(r => r.Grade.HasValue).Any() 
                ? courseReports.Where(r => r.Grade.HasValue).Average(r => r.Grade!.Value) 
                : (decimal?)null;

            coursePerformance.Add(new CoursePerformanceDto
            {
                CourseId = enrollment.CourseId,
                CourseName = course?.Name ?? "Unknown",
                AverageGrade = avgGrade,
                AssignmentsCompleted = courseReports.Count,
                AssignmentsTotal = courseAssignments.Count,
                CompletionRate = courseAssignments.Count > 0 ? (decimal)courseReports.Count / courseAssignments.Count * 100 : 0
            });
        }

        // Topic performance
        var topicPerformance = new List<TopicPerformanceDto>();
        var topicGroups = allAssignments.GroupBy(a => a.TopicId);

        foreach (var topicGroup in topicGroups)
        {
            var topic = await _unitOfWork.Topics.GetByIdAsync(topicGroup.Key, cancellationToken);
            var topicReports = allReports.Where(r => topicGroup.Select(a => a.Id).Contains(r.AssignmentId) && r.Grade.HasValue).ToList();

            if (!topicReports.Any()) continue;

            var avgGrade = topicReports.Average(r => r.Grade!.Value);
            var performanceLevel = avgGrade switch
            {
                >= 90 => "Excellent",
                >= 80 => "Good",
                >= 70 => "Average",
                _ => "NeedsImprovement"
            };

            topicPerformance.Add(new TopicPerformanceDto
            {
                TopicName = topic?.Name ?? "Unknown",
                AverageGrade = avgGrade,
                AssignmentsCount = topicReports.Count,
                PerformanceLevel = performanceLevel
            });
        }

        // Resubmission rate
        var resubmissions = allReports.Count(r => r.Status == ReportStatus.Resubmitted);

        var analytics = new StudentPerformanceAnalyticsDto
        {
            OnTimeSubmissionRate = allReports.Count() > 0 ? (decimal)onTimeSubmissions / allReports.Count() * 100 : 0,
            LateSubmissionRate = allReports.Count() > 0 ? (decimal)lateSubmissions / allReports.Count() * 100 : 0,
            TotalSubmissions = allReports.Count(),
            OnTimeSubmissions = onTimeSubmissions,
            LateSubmissions = lateSubmissions,
            AverageGrade = averageGrade,
            CoursePerformance = coursePerformance,
            TopicPerformance = topicPerformance,
            ResubmissionRate = allReports.Count() > 0 ? (decimal)resubmissions / allReports.Count() * 100 : 0,
            TotalResubmissions = resubmissions
        };

        return new DashboardResponse<StudentPerformanceAnalyticsDto>
        {
            Success = true,
            Data = analytics
        };
    }
}

// Handler: Get Student Grade Breakdown with Weighted Contributions
public class GetStudentGradeBreakdownQueryHandler 
    : IRequestHandler<GetStudentGradeBreakdownQuery, DashboardResponse<StudentGradeBreakdownDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetStudentGradeBreakdownQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<DashboardResponse<StudentGradeBreakdownDto>> Handle(
        GetStudentGradeBreakdownQuery request, 
        CancellationToken cancellationToken)
    {
        var studentId = _currentUserService.UserId!.Value;

        // Get course
        var course = await _unitOfWork.Courses.GetByIdAsync(request.CourseId, cancellationToken);
        if (course == null)
        {
            return new DashboardResponse<StudentGradeBreakdownDto>
            {
                Success = false,
                Message = "Course not found"
            };
        }

        var courseCode = await _unitOfWork.CourseCodes.GetByIdAsync(course.CourseCodeId, cancellationToken);

        // Get assignments
        var assignments = await _unitOfWork.Assignments
            .GetManyAsync(a => a.CourseId == request.CourseId, cancellationToken);

        var assignmentBreakdown = new List<AssignmentGradeDetailDto>();
        decimal totalWeightedScore = 0;
        decimal totalWeightUsed = 0;

        foreach (var assignment in assignments)
        {
            // Get student's report for this assignment
            var report = await StudentReportHelper.GetStudentReportForAssignmentAsync(
                _unitOfWork,
                studentId,
                assignment.Id,
                cancellationToken);

            if (report == null) continue;

            var topic = await _unitOfWork.Topics.GetByIdAsync(assignment.TopicId, cancellationToken);
            var weight = assignment.WeightPercentageSnapshot ?? 0m;
            var weightedContribution = 0m;

            if (report.Grade.HasValue)
            {
                weightedContribution = (decimal)report.Grade.Value * (weight / 100m);
                totalWeightedScore += weightedContribution;
                totalWeightUsed += weight;
            }

            assignmentBreakdown.Add(new AssignmentGradeDetailDto
            {
                AssignmentId = assignment.Id,
                AssignmentTitle = assignment.Title,
                TopicName = topic?.Name ?? "Unknown",
                Grade = report.Grade,
                MaxPoints = assignment.MaxPoints ?? 100,
                Weight = weight,
                WeightedContribution = weightedContribution,
                SubmittedAt = report.SubmittedAt,
                GradedAt = report.GradedAt,
                Status = report.Status.ToString()
            });
        }

        var letterGrade = CalculateLetterGrade(totalWeightedScore);

        return new DashboardResponse<StudentGradeBreakdownDto>
        {
            Success = true,
            Data = new StudentGradeBreakdownDto
            {
                CourseId = course.Id,
                CourseName = course.Name,
                CourseCode = courseCode?.Code ?? "N/A",
                AssignmentBreakdown = assignmentBreakdown.OrderBy(a => a.SubmittedAt ?? DateTime.MaxValue).ToList(),
                WeightedCourseGrade = totalWeightedScore,
                LetterGrade = letterGrade,
                TotalWeightUsed = totalWeightUsed,
                RemainingWeight = 100 - totalWeightUsed
            }
        };
    }

    private static string CalculateLetterGrade(decimal grade)
    {
        if (grade >= 90) return "A";
        if (grade >= 80) return "B";
        if (grade >= 70) return "C";
        if (grade >= 60) return "D";
        return "F";
    }
}
