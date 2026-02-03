using MediatR;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Application.Features.Assignments.Queries;

public class GetAssignmentStatisticsQueryHandler : IRequestHandler<GetAssignmentStatisticsQuery, GetAssignmentStatisticsResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAssignmentStatisticsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GetAssignmentStatisticsResponse> Handle(GetAssignmentStatisticsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var course = await _unitOfWork.Courses.GetByIdAsync(request.CourseId, cancellationToken);

            if (course == null)
            {
                return new GetAssignmentStatisticsResponse
                {
                    Success = false,
                    Message = "Course not found",
                    CourseId = request.CourseId
                };
            }

            var assignments = await _unitOfWork.Assignments.GetAssignmentsByCourseAsync(request.CourseId, cancellationToken);
            var assignmentsList = assignments.ToList();

            var totalAssignments = assignmentsList.Count;

            if (totalAssignments == 0)
            {
                return new GetAssignmentStatisticsResponse
                {
                    Success = true,
                    Message = "No assignments found for this course",
                    CourseId = request.CourseId,
                    CourseName = course.Name ?? "Unknown Course",
                    TotalAssignments = 0,
                    ByStatus = new Dictionary<string, int>(),
                    IndividualAssignments = 0,
                    GroupAssignments = 0,
                    UpcomingAssignments = 0,
                    OverdueAssignments = 0,
                    ActiveAssignments = 0,
                    TotalGroupsAssigned = 0,
                    AssignmentsWithGroups = 0,
                    AssignmentsWithoutGroups = 0
                };
            }

            var now = DateTime.UtcNow;
            var upcomingDate = now.AddDays(7);

            // Status statistics
            var byStatus = assignmentsList
                .GroupBy(a => a.Status.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            // Type statistics
            var individualCount = assignmentsList.Count(a => !a.IsGroupAssignment);
            var groupCount = assignmentsList.Count(a => a.IsGroupAssignment);

            // Time-based statistics
            var upcomingCount = assignmentsList.Count(a => 
                a.DueDate >= now && a.DueDate <= upcomingDate);
            
            var overdueCount = assignmentsList.Count(a => 
                (a.ExtendedDueDate ?? a.DueDate) < now);
            
            var activeCount = assignmentsList.Count(a => 
                a.Status == AssignmentStatus.Active);

            // Group statistics (null-safe)
            var totalGroupsAssigned = assignmentsList.Sum(a => a.AssignedGroups?.Count ?? 0);
            var assignmentsWithGroups = assignmentsList.Count(a => a.AssignedGroups != null && a.AssignedGroups.Any());
            var assignmentsWithoutGroups = totalAssignments - assignmentsWithGroups;

            // Date statistics
            var earliestDueDate = assignmentsList.Min(a => a.DueDate);
            var latestDueDate = assignmentsList.Max(a => a.DueDate);
            
            var averageDaysUntilDue = assignmentsList
                .Select(a => (a.ExtendedDueDate.HasValue ? a.ExtendedDueDate.Value : a.DueDate) - now)
                .Select(ts => ts.TotalDays)
                .Average();

            return new GetAssignmentStatisticsResponse
            {
                Success = true,
                Message = "Statistics retrieved successfully",
                CourseId = request.CourseId,
                CourseName = course.Name ?? "Unknown Course",
                TotalAssignments = totalAssignments,
                ByStatus = byStatus,
                IndividualAssignments = individualCount,
                GroupAssignments = groupCount,
                UpcomingAssignments = upcomingCount,
                OverdueAssignments = overdueCount,
                ActiveAssignments = activeCount,
                TotalGroupsAssigned = totalGroupsAssigned,
                AssignmentsWithGroups = assignmentsWithGroups,
                AssignmentsWithoutGroups = assignmentsWithoutGroups,
                EarliestDueDate = earliestDueDate,
                LatestDueDate = latestDueDate,
                AverageDaysUntilDue = averageDaysUntilDue
            };
        }
        catch (Exception ex)
        {
            return new GetAssignmentStatisticsResponse
            {
                Success = false,
                Message = $"Error retrieving statistics: {ex.Message}",
                CourseId = request.CourseId
            };
        }
    }
}
