using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Application.Features.Dashboard.Helpers;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.Dashboard.Queries;

public class GetStudentCourseGradesQueryHandler 
    : IRequestHandler<GetStudentCourseGradesQuery, DashboardResponse<CourseGradesDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITopicWeightService _topicWeightService;

    public GetStudentCourseGradesQueryHandler(
        IUnitOfWork unitOfWork, 
        ICurrentUserService currentUserService,
        ITopicWeightService topicWeightService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _topicWeightService = topicWeightService;
    }

    public async Task<DashboardResponse<CourseGradesDetailDto>> Handle(
        GetStudentCourseGradesQuery request, 
        CancellationToken cancellationToken)
    {
        var studentId = _currentUserService.UserId!.Value;

        // Verify student is enrolled
        var enrollment = await _unitOfWork.CourseEnrollments
            .GetAsync(e => e.StudentId == studentId && e.CourseId == request.CourseId, cancellationToken);

        if (enrollment == null)
        {
            return new DashboardResponse<CourseGradesDetailDto>
            {
                Success = false,
                Message = "Student not enrolled in this course"
            };
        }

        var course = await _unitOfWork.Courses.GetByIdAsync(request.CourseId, cancellationToken);
        var courseCode = course != null ? await _unitOfWork.CourseCodes.GetByIdAsync(course.CourseCodeId, cancellationToken) : null;

        // Get all assignments
        var assignments = await _unitOfWork.Assignments
            .GetManyAsync(a => a.CourseId == request.CourseId, cancellationToken);

        // Get student's reports (includes both individual and group submissions)
        var reports = await StudentReportHelper.GetStudentReportsAsync(
            _unitOfWork,
            studentId,
            assignments.Select(a => a.Id),
            cancellationToken);

        var assignmentGrades = new List<DashboardAssignmentGradeDto>();
        var topicBreakdown = new Dictionary<string, decimal>();
        var gradeTrend = new List<GradeTrendPoint>();

        foreach (var assignment in assignments.OrderBy(a => a.DueDate))
        {
            var report = reports.FirstOrDefault(r => r.AssignmentId == assignment.Id);
            var topic = await _unitOfWork.Topics.GetByIdAsync(assignment.TopicId, cancellationToken);

            var assignmentGrade = new DashboardAssignmentGradeDto
            {
                AssignmentId = assignment.Id,
                Title = assignment.Title,
                TopicName = topic?.Name ?? "N/A",
                Grade = report?.Grade,
                MaxPoints = assignment.MaxPoints,
                SubmittedAt = report?.SubmittedAt,
                GradedAt = report?.GradedAt,
                Status = report?.Status.ToString() ?? "Not Submitted",
                Feedback = report?.Feedback
            };

            assignmentGrades.Add(assignmentGrade);

            // Topic breakdown
            if (report?.Grade.HasValue == true && topic != null)
            {
                if (!topicBreakdown.ContainsKey(topic.Name))
                    topicBreakdown[topic.Name] = 0;

                topicBreakdown[topic.Name] += report.Grade.Value;
            }

            // Grade trend
            if (report?.GradedAt.HasValue == true && report.Grade.HasValue)
            {
                gradeTrend.Add(new GradeTrendPoint
                {
                    Date = report.GradedAt.Value,
                    Grade = report.Grade,
                    AssignmentTitle = assignment.Title
                });
            }
        }

        // Calculate average grade using topic weights
        decimal? averageGrade = await CalculateWeightedAverageGradeAsync(request.CourseId, studentId, cancellationToken);

        var detail = new CourseGradesDetailDto
        {
            CourseId = request.CourseId,
            CourseName = course?.Name ?? "Unknown",
            CourseCode = courseCode?.Code ?? "N/A",
            AverageGrade = averageGrade,
            LetterGrade = ConvertToLetterGrade(averageGrade),
            Assignments = assignmentGrades,
            TopicBreakdown = topicBreakdown,
            GradeTrend = gradeTrend,
            ClassAverage = null // Would require calculating across all students
        };

        return new DashboardResponse<CourseGradesDetailDto>
        {
            Success = true,
            Data = detail
        };
    }

    private async Task<decimal?> CalculateWeightedAverageGradeAsync(Guid courseId, Guid studentId, CancellationToken cancellationToken)
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
}
