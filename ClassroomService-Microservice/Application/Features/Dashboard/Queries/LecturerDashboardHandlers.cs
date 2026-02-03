using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.Dashboard.Queries;

// Handler 1: Get Lecturer Courses Overview
public class GetLecturerCoursesOverviewQueryHandler 
    : IRequestHandler<GetLecturerCoursesOverviewQuery, DashboardResponse<LecturerCoursesOverviewDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetLecturerCoursesOverviewQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<DashboardResponse<LecturerCoursesOverviewDto>> Handle(
        GetLecturerCoursesOverviewQuery request, 
        CancellationToken cancellationToken)
    {
        var lecturerId = _currentUserService.UserId!.Value;

        // Get lecturer's courses
        var courses = await _unitOfWork.Courses
            .GetManyAsync(c => c.LecturerId == lecturerId, cancellationToken);

        // Filter by term if specified
        if (request.TermId.HasValue)
        {
            courses = courses.Where(c => c.TermId == request.TermId.Value).ToList();
        }
        else
        {
            // Get current term courses
            var currentTerm = await _unitOfWork.Terms
                .GetAsync(t => t.IsActive && t.StartDate <= DateTime.UtcNow && t.EndDate >= DateTime.UtcNow, cancellationToken);
            
            if (currentTerm != null)
            {
                courses = courses.Where(c => c.TermId == currentTerm.Id).ToList();
            }
        }

        var lecturerCourses = new List<LecturerCourseDto>();
        int totalStudents = 0;
        int totalPendingGrading = 0;
        int totalActiveAssignments = 0;

        foreach (var course in courses)
        {
            var courseCode = await _unitOfWork.CourseCodes.GetByIdAsync(course.CourseCodeId, cancellationToken);
            var term = await _unitOfWork.Terms.GetByIdAsync(course.TermId, cancellationToken);

            // Get enrollments
            var enrollments = await _unitOfWork.CourseEnrollments
                .GetManyAsync(e => e.CourseId == course.Id && e.Status == EnrollmentStatus.Active, cancellationToken);

            // Get assignments
            var assignments = await _unitOfWork.Assignments
                .GetManyAsync(a => a.CourseId == course.Id, cancellationToken);

            var activeAssignments = assignments.Count(a => a.Status == AssignmentStatus.Active || 
                                                           a.Status == AssignmentStatus.Extended);

            // Get pending reports
            var pendingReports = await _unitOfWork.Reports
                .GetManyAsync(r => assignments.Select(a => a.Id).Contains(r.AssignmentId) &&
                                  (r.Status == ReportStatus.Submitted || r.Status == ReportStatus.Resubmitted),
                             cancellationToken);

            // Get latest submission
            var allReports = await _unitOfWork.Reports
                .GetManyAsync(r => assignments.Select(a => a.Id).Contains(r.AssignmentId), cancellationToken);
            
            var lastSubmission = allReports.OrderByDescending(r => r.SubmittedAt).FirstOrDefault();

            // Calculate average grade
            var gradedReports = allReports.Where(r => r.Grade.HasValue).ToList();
            var avgGrade = gradedReports.Any() ? gradedReports.Average(r => r.Grade!.Value) : (decimal?)null;

            lecturerCourses.Add(new LecturerCourseDto
            {
                CourseId = course.Id,
                CourseName = course.Name,
                CourseCode = courseCode?.Code ?? "N/A",
                TermName = term?.Name ?? "N/A",
                EnrollmentCount = enrollments.Count(),
                PendingGradingCount = pendingReports.Count(),
                ActiveAssignmentsCount = activeAssignments,
                AverageCourseGrade = avgGrade,
                LastSubmissionDate = lastSubmission?.SubmittedAt
            });

            totalStudents += enrollments.Count();
            totalPendingGrading += pendingReports.Count();
            totalActiveAssignments += activeAssignments;
        }

        return new DashboardResponse<LecturerCoursesOverviewDto>
        {
            Success = true,
            Data = new LecturerCoursesOverviewDto
            {
                Courses = lecturerCourses,
                TotalStudentsEnrolled = totalStudents,
                TotalReportsPendingGrading = totalPendingGrading,
                TotalActiveAssignments = totalActiveAssignments
            }
        };
    }
}

// Handler 2: Get Grading Queue
public class GetGradingQueueQueryHandler 
    : IRequestHandler<GetGradingQueueQuery, DashboardResponse<GradingQueueDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IKafkaUserService _userService;

    public GetGradingQueueQueryHandler(
        IUnitOfWork unitOfWork, 
        ICurrentUserService currentUserService,
        IKafkaUserService userService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _userService = userService;
    }

    public async Task<DashboardResponse<GradingQueueDto>> Handle(
        GetGradingQueueQuery request, 
        CancellationToken cancellationToken)
    {
        var lecturerId = _currentUserService.UserId!.Value;

        // Get lecturer's courses
        var courses = await _unitOfWork.Courses
            .GetManyAsync(c => c.LecturerId == lecturerId, cancellationToken);

        // Filter by specific course if requested
        if (request.CourseId.HasValue)
        {
            courses = courses.Where(c => c.Id == request.CourseId.Value).ToList();
        }

        var courseIds = courses.Select(c => c.Id).ToList();

        // Get all assignments
        var assignments = await _unitOfWork.Assignments
            .GetManyAsync(a => courseIds.Contains(a.CourseId), cancellationToken);

        // Get pending reports
        var pendingReports = await _unitOfWork.Reports
            .GetManyAsync(r => assignments.Select(a => a.Id).Contains(r.AssignmentId) &&
                              (r.Status == ReportStatus.Submitted || r.Status == ReportStatus.Resubmitted),
                         cancellationToken);

        var pendingGradingList = new List<PendingGradingReportDto>();

        foreach (var report in pendingReports.OrderBy(r => r.SubmittedAt))
        {
            var assignment = assignments.FirstOrDefault(a => a.Id == report.AssignmentId);
            var course = courses.FirstOrDefault(c => c.Id == assignment?.CourseId);

            string submitterName = "Unknown";
            string? groupName = null;

            if (report.IsGroupSubmission && report.GroupId.HasValue)
            {
                var group = await _unitOfWork.Groups.GetByIdAsync(report.GroupId.Value, cancellationToken);
                groupName = group?.Name;
            }

            var submitter = await _userService.GetUserByIdAsync(report.SubmittedBy, cancellationToken);
            submitterName = submitter?.FullName ?? "Unknown";

            var daysSince = report.SubmittedAt.HasValue 
                ? (int)(DateTime.UtcNow - report.SubmittedAt.Value).TotalDays 
                : 0;

            pendingGradingList.Add(new PendingGradingReportDto
            {
                ReportId = report.Id,
                AssignmentId = assignment?.Id ?? Guid.Empty,
                AssignmentTitle = assignment?.Title ?? "Unknown",
                CourseName = course?.Name ?? "Unknown",
                Status = report.Status.ToString(),
                SubmittedAt = report.SubmittedAt ?? DateTime.UtcNow,
                DaysSinceSubmission = daysSince,
                IsGroupSubmission = report.IsGroupSubmission,
                GroupName = groupName,
                SubmitterName = submitterName,
                Version = report.Version
            });
        }

        return new DashboardResponse<GradingQueueDto>
        {
            Success = true,
            Data = new GradingQueueDto
            {
                PendingReports = pendingGradingList,
                TotalPending = pendingGradingList.Count,
                SubmittedCount = pendingGradingList.Count(p => p.Status == "Submitted"),
                ResubmittedCount = pendingGradingList.Count(p => p.Status == "Resubmitted")
            }
        };
    }
}

// Handler 3: Get Course Student Performance
public class GetCourseStudentPerformanceQueryHandler 
    : IRequestHandler<GetCourseStudentPerformanceQuery, DashboardResponse<CourseStudentPerformanceDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IKafkaUserService _userService;

    public GetCourseStudentPerformanceQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IKafkaUserService userService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _userService = userService;
    }

    public async Task<DashboardResponse<CourseStudentPerformanceDto>> Handle(
        GetCourseStudentPerformanceQuery request, 
        CancellationToken cancellationToken)
    {
        var lecturerId = _currentUserService.UserId!.Value;

        // Verify lecturer owns this course
        var course = await _unitOfWork.Courses.GetByIdAsync(request.CourseId, cancellationToken);
        if (course == null || course.LecturerId != lecturerId)
        {
            return new DashboardResponse<CourseStudentPerformanceDto>
            {
                Success = false,
                Message = "Course not found or unauthorized"
            };
        }

        // Get enrollments
        var enrollments = await _unitOfWork.CourseEnrollments
            .GetManyAsync(e => e.CourseId == request.CourseId && e.Status == EnrollmentStatus.Active, cancellationToken);

        // Get assignments
        var assignments = await _unitOfWork.Assignments
            .GetManyAsync(a => a.CourseId == request.CourseId, cancellationToken);

        // Get all reports
        var reports = await _unitOfWork.Reports
            .GetManyAsync(r => assignments.Select(a => a.Id).Contains(r.AssignmentId), cancellationToken);

        // Calculate grade distribution
        var gradeDistribution = new GradeDistributionDto();
        var studentGrades = new Dictionary<Guid, List<decimal>>();

        foreach (var report in reports.Where(r => r.Grade.HasValue))
        {
            var grade = report.Grade!.Value;
            
            // Distribution by grade
            if (grade >= 90) gradeDistribution.ACount++;
            else if (grade >= 80) gradeDistribution.BCount++;
            else if (grade >= 70) gradeDistribution.CCount++;
            else if (grade >= 60) gradeDistribution.DCount++;
            else gradeDistribution.FCount++;

            // Track student grades
            if (!studentGrades.ContainsKey(report.SubmittedBy))
                studentGrades[report.SubmittedBy] = new List<decimal>();
            
            studentGrades[report.SubmittedBy].Add(grade);
        }

        // Assignment performance
        var assignmentPerformance = new List<AssignmentPerformanceDto>();
        foreach (var assignment in assignments)
        {
            var assignmentReports = reports.Where(r => r.AssignmentId == assignment.Id).ToList();
            var gradedReports = assignmentReports.Where(r => r.Grade.HasValue).ToList();

            assignmentPerformance.Add(new AssignmentPerformanceDto
            {
                AssignmentId = assignment.Id,
                Title = assignment.Title,
                AverageGrade = gradedReports.Any() ? gradedReports.Average(r => r.Grade!.Value) : null,
                SubmissionCount = assignmentReports.Count,
                TotalStudents = enrollments.Count(),
                SubmissionRate = enrollments.Count() > 0 ? (decimal)assignmentReports.Count / enrollments.Count() * 100 : 0
            });
        }

        // Top performers and at-risk students - using WEIGHTED grades
        var studentSummaries = new List<StudentSummaryDto>();
        var studentWeightedGrades = new List<decimal>();
        
        foreach (var enrollment in enrollments)
        {
            var studentReports = reports.Where(r => r.SubmittedBy == enrollment.StudentId).ToList();
            var gradedReports = studentReports.Where(r => r.Grade.HasValue).ToList();
            
            // Calculate WEIGHTED grade using assignment weight snapshots
            decimal? weightedGrade = null;
            if (gradedReports.Any())
            {
                decimal totalWeightedScore = 0;
                
                foreach (var report in gradedReports)
                {
                    var assignment = assignments.FirstOrDefault(a => a.Id == report.AssignmentId);
                    if (assignment != null && report.Grade.HasValue)
                    {
                        var weightPercentage = (assignment.WeightPercentageSnapshot ?? 0m) / 100m;
                        totalWeightedScore += (decimal)report.Grade.Value * weightPercentage;
                    }
                }
                
                weightedGrade = totalWeightedScore;
                studentWeightedGrades.Add(totalWeightedScore);
            }
            
            var lateCount = studentReports.Count(r => r.Status == ReportStatus.Late);
            var student = await _userService.GetUserByIdAsync(enrollment.StudentId, cancellationToken);

            var riskLevel = "Low";
            if (weightedGrade < 60 || lateCount > 2) riskLevel = "High";
            else if (weightedGrade < 70 || lateCount > 0) riskLevel = "Medium";

            studentSummaries.Add(new StudentSummaryDto
            {
                StudentId = enrollment.StudentId,
                StudentName = student?.FullName ?? "Unknown",
                AverageGrade = weightedGrade,
                AssignmentsCompleted = gradedReports.Count,
                AssignmentsTotal = assignments.Count(),
                LateSubmissions = lateCount,
                RiskLevel = riskLevel
            });
        }

        var topPerformers = studentSummaries
            .Where(s => s.AverageGrade.HasValue)
            .OrderByDescending(s => s.AverageGrade)
            .Take(5)
            .ToList();

        var atRiskStudents = studentSummaries
            .Where(s => s.RiskLevel == "High" || s.RiskLevel == "Medium")
            .OrderBy(s => s.AverageGrade)
            .ToList();

        // Use WEIGHTED course average instead of simple average
        var avgCourseGrade = studentWeightedGrades.Any() 
            ? studentWeightedGrades.Average() 
            : 0;

        var submissionRate = assignments.Count() > 0 && enrollments.Count() > 0
            ? (decimal)reports.Count() / (assignments.Count() * enrollments.Count()) * 100
            : 0;

        return new DashboardResponse<CourseStudentPerformanceDto>
        {
            Success = true,
            Data = new CourseStudentPerformanceDto
            {
                CourseId = request.CourseId,
                CourseName = course.Name,
                GradeDistribution = gradeDistribution,
                AssignmentPerformance = assignmentPerformance,
                TopPerformers = topPerformers,
                AtRiskStudents = atRiskStudents,
                AverageCourseGrade = avgCourseGrade,
                SubmissionRate = submissionRate,
                TotalStudents = enrollments.Count()
            }
        };
    }
}

// Handler 4: Get Assignment Statistics
public class GetAssignmentStatisticsQueryHandler 
    : IRequestHandler<GetAssignmentStatisticsQuery, DashboardResponse<AssignmentStatisticsDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetAssignmentStatisticsQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<DashboardResponse<AssignmentStatisticsDto>> Handle(
        GetAssignmentStatisticsQuery request, 
        CancellationToken cancellationToken)
    {
        var lecturerId = _currentUserService.UserId!.Value;

        // Verify ownership
        var course = await _unitOfWork.Courses.GetByIdAsync(request.CourseId, cancellationToken);
        if (course == null || course.LecturerId != lecturerId)
        {
            return new DashboardResponse<AssignmentStatisticsDto>
            {
                Success = false,
                Message = "Course not found or unauthorized"
            };
        }

        // Get enrollments
        var enrollments = await _unitOfWork.CourseEnrollments
            .GetManyAsync(e => e.CourseId == request.CourseId && e.Status == EnrollmentStatus.Active, cancellationToken);

        // Get assignments
        var assignments = await _unitOfWork.Assignments
            .GetManyAsync(a => a.CourseId == request.CourseId, cancellationToken);

        // Get reports
        var reports = await _unitOfWork.Reports
            .GetManyAsync(r => assignments.Select(a => a.Id).Contains(r.AssignmentId), cancellationToken);

        var assignmentStats = new List<AssignmentStatsDto>();

        foreach (var assignment in assignments)
        {
            var topic = await _unitOfWork.Topics.GetByIdAsync(assignment.TopicId, cancellationToken);
            var assignmentReports = reports.Where(r => r.AssignmentId == assignment.Id).ToList();
            var onTime = assignmentReports.Count(r => r.SubmittedAt <= assignment.DueDate);
            var late = assignmentReports.Count - onTime;

            var gradedReports = assignmentReports.Where(r => r.Grade.HasValue).ToList();
            var avgGrade = gradedReports.Any() ? gradedReports.Average(r => r.Grade!.Value) : (decimal?)null;
            var lowestGrade = gradedReports.Any() ? gradedReports.Min(r => r.Grade!.Value) : (decimal?)null;
            var highestGrade = gradedReports.Any() ? gradedReports.Max(r => r.Grade!.Value) : (decimal?)null;

            // Determine difficulty based on average grade and submission rate
            var submissionRate = enrollments.Count() > 0 ? (decimal)assignmentReports.Count / enrollments.Count() * 100 : 0;
            var difficultyLevel = "Medium";
            
            if (avgGrade >= 80 && submissionRate >= 90) difficultyLevel = "Easy";
            else if (avgGrade < 65 || submissionRate < 60) difficultyLevel = "Hard";

            assignmentStats.Add(new AssignmentStatsDto
            {
                AssignmentId = assignment.Id,
                Title = assignment.Title,
                TopicName = topic?.Name ?? "N/A",
                TotalSubmissions = assignmentReports.Count,
                ExpectedSubmissions = enrollments.Count(),
                SubmissionRate = submissionRate,
                OnTimeSubmissions = onTime,
                LateSubmissions = late,
                AverageGrade = avgGrade,
                LowestGrade = lowestGrade,
                HighestGrade = highestGrade,
                DifficultyLevel = difficultyLevel
            });
        }

        var overallSubmissionRate = assignments.Count() > 0 && enrollments.Count() > 0
            ? (decimal)reports.Count() / (assignments.Count() * enrollments.Count()) * 100
            : 0;

        var overallAvgGrade = reports.Where(r => r.Grade.HasValue).Any()
            ? reports.Where(r => r.Grade.HasValue).Average(r => r.Grade!.Value)
            : 0;

        return new DashboardResponse<AssignmentStatisticsDto>
        {
            Success = true,
            Data = new AssignmentStatisticsDto
            {
                CourseId = request.CourseId,
                CourseName = course.Name,
                Assignments = assignmentStats,
                OverallSubmissionRate = overallSubmissionRate,
                OverallAverageGrade = overallAvgGrade
            }
        };
    }
}
