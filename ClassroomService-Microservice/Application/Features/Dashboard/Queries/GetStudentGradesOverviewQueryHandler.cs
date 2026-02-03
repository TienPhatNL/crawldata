using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Application.Features.Dashboard.Helpers;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClassroomService.Application.Features.Dashboard.Queries;

public class GetStudentGradesOverviewQueryHandler 
    : IRequestHandler<GetStudentGradesOverviewQuery, DashboardResponse<StudentGradesOverviewDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IKafkaUserService _userService;
    private readonly ITopicWeightService _topicWeightService;

    public GetStudentGradesOverviewQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IKafkaUserService userService,
        ITopicWeightService topicWeightService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _userService = userService;
        _topicWeightService = topicWeightService;
    }

    public async Task<DashboardResponse<StudentGradesOverviewDto>> Handle(
        GetStudentGradesOverviewQuery request, 
        CancellationToken cancellationToken)
    {
        var studentId = _currentUserService.UserId!.Value;

        // Get student's enrollments
        var enrollments = await _unitOfWork.CourseEnrollments
            .GetManyAsync(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Active, cancellationToken);

        // Filter by term if specified, otherwise get current term
        if (request.TermId.HasValue)
        {
            var termCourseIds = await _unitOfWork.Courses
                .GetManyAsync(c => c.TermId == request.TermId.Value, cancellationToken);
            var termCourseIdSet = termCourseIds.Select(c => c.Id).ToHashSet();
            enrollments = enrollments.Where(e => termCourseIdSet.Contains(e.CourseId)).ToList();
        }
        else
        {
            // Get current term (most recent active term)
            var currentTerm = await _unitOfWork.Terms
                .GetAsync(t => t.IsActive && t.StartDate <= DateTime.UtcNow && t.EndDate >= DateTime.UtcNow, cancellationToken);
            
            if (currentTerm != null)
            {
                var termCourseIds = await _unitOfWork.Courses
                    .GetManyAsync(c => c.TermId == currentTerm.Id, cancellationToken);
                var termCourseIdSet = termCourseIds.Select(c => c.Id).ToHashSet();
                enrollments = enrollments.Where(e => termCourseIdSet.Contains(e.CourseId)).ToList();
            }
        }

        var courseGrades = new List<CourseGradeSummaryDto>();
        var gradeDistribution = new GradeDistributionDto();
        decimal totalGradePoints = 0;
        int gradedCoursesCount = 0;

        foreach (var enrollment in enrollments)
        {
            var course = await _unitOfWork.Courses.GetByIdAsync(enrollment.CourseId, cancellationToken);
            if (course == null) continue;

            var courseCode = await _unitOfWork.CourseCodes.GetByIdAsync(course.CourseCodeId, cancellationToken);
            var term = await _unitOfWork.Terms.GetByIdAsync(course.TermId, cancellationToken);

            // Get all assignments for this course
            var assignments = await _unitOfWork.Assignments
                .GetManyAsync(a => a.CourseId == course.Id, cancellationToken);

            // Get student's reports for this course (includes both individual and group submissions)
            var reports = await StudentReportHelper.GetStudentReportsAsync(
                _unitOfWork,
                studentId,
                assignments.Select(a => a.Id),
                cancellationToken);

            // Calculate weighted grade for course using topic weights
            decimal? courseGrade = await CalculateCourseGradeAsync(course.Id, studentId, cancellationToken);

            var completedAssignments = reports.Count(r => r.Status == ReportStatus.Graded);
            var totalAssignments = assignments.Count();

            var courseGradeSummary = new CourseGradeSummaryDto
            {
                CourseId = course.Id,
                CourseName = course.Name,
                CourseCode = courseCode?.Code ?? "N/A",
                TermName = term?.Name ?? "N/A",
                CurrentGrade = courseGrade,
                LetterGrade = ConvertToLetterGrade(courseGrade),
                CompletedAssignments = completedAssignments,
                TotalAssignments = totalAssignments,
                CompletionRate = totalAssignments > 0 ? (decimal)completedAssignments / totalAssignments * 100 : 0
            };

            courseGrades.Add(courseGradeSummary);

            // Update grade distribution
            if (courseGrade.HasValue)
            {
                UpdateGradeDistribution(gradeDistribution, courseGrade.Value);
                totalGradePoints += courseGrade.Value;
                gradedCoursesCount++;
            }
            else
            {
                gradeDistribution.UngradeCount++;
            }
        }

        var currentTermGpa = gradedCoursesCount > 0 ? totalGradePoints / gradedCoursesCount / 25 : (decimal?)null; // Assuming 100-point scale, convert to 4.0 scale

        var overview = new StudentGradesOverviewDto
        {
            CurrentTermGpa = currentTermGpa,
            OverallGpa = currentTermGpa, // For now, same as current term. Can be extended to calculate across all terms
            Courses = courseGrades,
            GradeDistribution = gradeDistribution
        };

        return new DashboardResponse<StudentGradesOverviewDto>
        {
            Success = true,
            Data = overview,
            GeneratedAt = DateTime.UtcNow
        };
    }

    private async Task<decimal?> CalculateCourseGradeAsync(Guid courseId, Guid studentId, CancellationToken cancellationToken)
    {
        try
        {
            var grade = await _topicWeightService.CalculateWeightedGradeAsync(courseId, studentId);
            return grade > 0 ? grade : null;
        }
        catch
        {
            return null;
        }
    }

    private string? ConvertToLetterGrade(decimal? grade)
    {
        if (!grade.HasValue) return null;

        return grade.Value switch
        {
            >= 90 => "A",
            >= 80 => "B",
            >= 70 => "C",
            >= 60 => "D",
            _ => "F"
        };
    }

    private void UpdateGradeDistribution(GradeDistributionDto distribution, decimal grade)
    {
        switch (grade)
        {
            case >= 90:
                distribution.ACount++;
                break;
            case >= 80:
                distribution.BCount++;
                break;
            case >= 70:
                distribution.CCount++;
                break;
            case >= 60:
                distribution.DCount++;
                break;
            default:
                distribution.FCount++;
                break;
        }
    }
}
