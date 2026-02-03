using ClassroomService.Application.Features.Assignments.DTOs;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Constants;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClassroomService.Application.Features.Assignments.Queries;

public class GetMyAssignmentsQueryHandler : IRequestHandler<GetMyAssignmentsQuery, GetMyAssignmentsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetMyAssignmentsQueryHandler> _logger;

    public GetMyAssignmentsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetMyAssignmentsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GetMyAssignmentsResponse> Handle(GetMyAssignmentsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Get courses the student is enrolled in
            var allEnrollments = await _unitOfWork.CourseEnrollments
                .GetEnrollmentsByStudentAsync(request.StudentId, cancellationToken);
            
            var activeEnrollments = allEnrollments.Where(e => e.Status == EnrollmentStatus.Active).ToList();
            var enrolledCourseIds = activeEnrollments.Select(e => e.CourseId).ToList();

            if (!enrolledCourseIds.Any())
            {
                return new GetMyAssignmentsResponse
                {
                    Success = true,
                    Message = "No enrolled courses found",
                    Assignments = new List<AssignmentSummaryDto>(),
                    TotalCount = 0,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalPages = 0
                };
            }

            // Get all assignments for enrolled courses
            var allAssignments = new List<Domain.Entities.Assignment>();
            foreach (var courseId in enrolledCourseIds)
            {
                var courseAssignments = await _unitOfWork.Assignments.GetAssignmentsByCourseAsync(courseId, cancellationToken);
                allAssignments.AddRange(courseAssignments);
            }

            // Filter out Draft assignments for students
            var relevantAssignments = allAssignments
                .Where(a => a.Status != AssignmentStatus.Draft)
                .ToList();

            // Apply filters
            if (request.CourseId.HasValue)
            {
                relevantAssignments = relevantAssignments.Where(a => a.CourseId == request.CourseId.Value).ToList();
            }

            if (request.Statuses != null && request.Statuses.Count > 0)
            {
                relevantAssignments = relevantAssignments.Where(a => request.Statuses.Contains(a.Status)).ToList();
            }

            if (request.IsUpcoming.HasValue && request.IsUpcoming.Value)
            {
                var upcomingDate = DateTime.UtcNow.AddDays(7);
                relevantAssignments = relevantAssignments
                    .Where(a => a.DueDate >= DateTime.UtcNow && a.DueDate <= upcomingDate).ToList();
            }

            if (request.IsOverdue.HasValue && request.IsOverdue.Value)
            {
                relevantAssignments = relevantAssignments
                    .Where(a => (a.ExtendedDueDate.HasValue ? a.ExtendedDueDate.Value : a.DueDate) < DateTime.UtcNow).ToList();
            }

            // Get total count before pagination
            var totalCount = relevantAssignments.Count;

            // Sorting
            relevantAssignments = request.SortBy.ToLower() switch
            {
                "title" => request.SortOrder.ToLower() == "desc" 
                    ? relevantAssignments.OrderByDescending(a => a.Title).ToList()
                    : relevantAssignments.OrderBy(a => a.Title).ToList(),
                "createdat" => request.SortOrder.ToLower() == "desc" 
                    ? relevantAssignments.OrderByDescending(a => a.CreatedAt).ToList()
                    : relevantAssignments.OrderBy(a => a.CreatedAt).ToList(),
                "status" => request.SortOrder.ToLower() == "desc" 
                    ? relevantAssignments.OrderByDescending(a => a.Status).ToList()
                    : relevantAssignments.OrderBy(a => a.Status).ToList(),
                _ => request.SortOrder.ToLower() == "desc" 
                    ? relevantAssignments.OrderByDescending(a => a.DueDate).ToList()
                    : relevantAssignments.OrderBy(a => a.DueDate).ToList()
            };

            // Pagination
            var assignments = relevantAssignments
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            // Build assignment DTOs with weight percentages
            var assignmentDtos = new List<AssignmentSummaryDto>();
            
            foreach (var a in assignments)
            {
                var effectiveDueDate = a.ExtendedDueDate ?? a.DueDate;
                var daysUntilDue = (int)(effectiveDueDate - DateTime.UtcNow).TotalDays;

                // Get all assigned group IDs if this is a group assignment
                List<Guid>? assignedGroupIds = a.IsGroupAssignment && a.AssignedGroups != null && a.AssignedGroups.Any()
                    ? a.AssignedGroups.Select(g => g.Id).ToList()
                    : null;

                // Use snapshot weight (captured at assignment creation time)
                decimal? weightPercentage = a.WeightPercentageSnapshot;

                assignmentDtos.Add(new AssignmentSummaryDto
                {
                    Id = a.Id,
                    CourseId = a.CourseId,
                    CourseName = a.Course?.Name ?? "Unknown Course",
                    TopicId = a.TopicId,
                    TopicName = a.Topic?.Name ?? "Unknown Topic",
                    Title = a.Title,
                    StartDate = a.StartDate,
                    DueDate = a.DueDate,
                    ExtendedDueDate = a.ExtendedDueDate,
                    Status = a.Status,
                    // Map Draft status to "Upcoming" for students
                    StatusDisplay = a.Status == AssignmentStatus.Draft ? "Upcoming" : a.Status.ToString(),
                    IsGroupAssignment = a.IsGroupAssignment,
                    MaxPoints = a.MaxPoints,
                    WeightPercentage = weightPercentage,
                    GroupIds = assignedGroupIds,
                    IsOverdue = effectiveDueDate < DateTime.UtcNow,
                    DaysUntilDue = daysUntilDue,
                    AssignedGroupsCount = a.AssignedGroups?.Count ?? 0,
                    CreatedAt = a.CreatedAt
                });
            }

            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            return new GetMyAssignmentsResponse
            {
                Success = true,
                Message = "Assignments retrieved successfullyy",
                Assignments = assignmentDtos,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = totalPages
            };
        }
        catch (Exception ex)
        {
            return new GetMyAssignmentsResponse
            {
                Success = false,
                Message = $"Error retrieving assignments: {ex.Message}",
                Assignments = new List<AssignmentSummaryDto>(),
                TotalCount = 0,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = 0
            };
        }
    }
}

