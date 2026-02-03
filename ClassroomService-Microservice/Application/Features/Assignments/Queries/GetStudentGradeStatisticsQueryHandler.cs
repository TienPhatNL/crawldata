using MediatR;
using Microsoft.EntityFrameworkCore;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Enums;
using ClassroomService.Application.Features.Assignments.DTOs;

namespace ClassroomService.Application.Features.Assignments.Queries;

public class GetStudentGradeStatisticsQueryHandler : IRequestHandler<GetStudentGradeStatisticsQuery, GetStudentGradeStatisticsResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetStudentGradeStatisticsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GetStudentGradeStatisticsResponse> Handle(GetStudentGradeStatisticsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Verify course exists
            var course = await _unitOfWork.Courses.GetByIdAsync(request.CourseId, cancellationToken);
            if (course == null)
            {
                return new GetStudentGradeStatisticsResponse
                {
                    Success = false,
                    Message = "Course not found"
                };
            }

            // Get student's enrollment in this course
            var enrollments = await _unitOfWork.CourseEnrollments.GetAllAsync(cancellationToken);
            var enrollment = enrollments.FirstOrDefault(e => 
                e.CourseId == request.CourseId && 
                e.StudentId == request.RequestUserId);

            if (enrollment == null)
            {
                return new GetStudentGradeStatisticsResponse
                {
                    Success = false,
                    Message = "You are not enrolled in this course"
                };
            }

            // Get all assignments for this course
            var allAssignments = await _unitOfWork.Assignments.GetAllAsync(cancellationToken);
            var assignments = allAssignments.Where(a => a.CourseId == request.CourseId).ToList();

            // Get student's reports (both individual and group)
            var allReports = await _unitOfWork.Reports.GetAllAsync(cancellationToken);
            
            // For individual assignments: reports where SubmittedBy = student
            var individualReports = allReports
                .Where(r => r.SubmittedBy == request.RequestUserId && r.Status == ReportStatus.Graded)
                .ToList();

            // For group assignments: find student's group, then get group reports
            var groupMembers = await _unitOfWork.GroupMembers.GetAllAsync(cancellationToken);
            var studentGroupIds = groupMembers
                .Where(gm => gm.EnrollmentId == enrollment.Id)
                .Select(gm => gm.GroupId)
                .ToHashSet();

            var groupReports = allReports
                .Where(r => r.GroupId.HasValue && 
                           studentGroupIds.Contains(r.GroupId.Value) && 
                           r.Status == ReportStatus.Graded)
                .ToList();

            // Combine all graded reports
            var allGradedReports = individualReports.Concat(groupReports).ToList();

            // Filter to only assignments that have graded reports
            var gradedAssignmentIds = allGradedReports.Select(r => r.AssignmentId).ToHashSet();
            var gradedAssignments = assignments
                .Where(a => gradedAssignmentIds.Contains(a.Id))
                .ToList();

            // Calculate assignment grades
            var assignmentGrades = new List<AssignmentGradeDto>();
            var totalPercentage = 0m;
            var gradeCount = 0;

            foreach (var assignment in gradedAssignments)
            {
                // Get the report for this assignment (could be individual or group)
                var report = allGradedReports.FirstOrDefault(r => r.AssignmentId == assignment.Id);
                if (report?.Grade == null || assignment.MaxPoints == null || assignment.MaxPoints == 0)
                    continue;

                var score = report.Grade.Value;
                var maxPoints = assignment.MaxPoints.Value;
                var percentage = (score / maxPoints) * 100m;

                assignmentGrades.Add(new AssignmentGradeDto
                {
                    AssignmentId = assignment.Id,
                    AssignmentName = assignment.Title,
                    DueDate = assignment.DueDate,
                    Score = score,
                    MaxPoints = maxPoints,
                    Percentage = Math.Round(percentage, 2),
                    Status = report.Status.ToString()
                });

                totalPercentage += percentage;
                gradeCount++;
            }

            var statistics = new StudentGradeStatisticsDto
            {
                CompletedAssignmentsCount = assignmentGrades.Count,
                AverageScore = gradeCount > 0 ? Math.Round(totalPercentage / gradeCount, 2) : 0,
                AssignmentGrades = assignmentGrades.OrderBy(a => a.DueDate).ToList()
            };

            return new GetStudentGradeStatisticsResponse
            {
                Success = true,
                Message = "Grade statistics retrieved successfully",
                Statistics = statistics
            };
        }
        catch (Exception ex)
        {
            return new GetStudentGradeStatisticsResponse
            {
                Success = false,
                Message = $"Error retrieving grade statistics: {ex.Message}"
            };
        }
    }

}
