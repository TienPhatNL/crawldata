using MediatR;
using ClassroomService.Application.Features.Assignments.DTOs;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Application.Features.Assignments.Queries;

/// <summary>
/// Handler for GetAssignmentsByTopicQuery
/// </summary>
public class GetAssignmentsByTopicQueryHandler : IRequestHandler<GetAssignmentsByTopicQuery, GetAssignmentsByTopicResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAssignmentsByTopicQueryHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GetAssignmentsByTopicResponse> Handle(GetAssignmentsByTopicQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate topic exists
            var topicExists = await _unitOfWork.Topics
                .ExistsAsync(t => t.Id == request.TopicId, cancellationToken);

            if (!topicExists)
            {
                return new GetAssignmentsByTopicResponse
                {
                    Success = false,
                    Message = "Topic not found",
                    Assignments = new List<AssignmentSummaryDto>(),
                    TotalCount = 0,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalPages = 0
                };
            }

            // Load assignments for the topic
            var topicAssignments = await _unitOfWork.Assignments
                .GetManyAsync(
                    a => a.TopicId == request.TopicId,
                    cancellationToken,
                    a => a.Course,
                    a => a.Course.CourseCode,
                    a => a.Topic,
                    a => a.AssignedGroups);

            // Materialize to list for in-memory filtering
            var assignmentsList = topicAssignments.ToList();

            // Apply filters
            if (request.CourseId.HasValue)
            {
                assignmentsList = assignmentsList.Where(a => a.CourseId == request.CourseId.Value).ToList();
            }

            if (request.Statuses != null && request.Statuses.Count > 0)
            {
                assignmentsList = assignmentsList.Where(a => request.Statuses.Contains(a.Status)).ToList();
            }

            if (request.IsGroupAssignment.HasValue)
            {
                assignmentsList = assignmentsList.Where(a => a.IsGroupAssignment == request.IsGroupAssignment.Value).ToList();
            }

            // Date filters
            if (request.DueDateFrom.HasValue)
            {
                assignmentsList = assignmentsList.Where(a => a.DueDate >= request.DueDateFrom.Value).ToList();
            }

            if (request.DueDateTo.HasValue)
            {
                assignmentsList = assignmentsList.Where(a => a.DueDate <= request.DueDateTo.Value).ToList();
            }

            if (request.IsUpcoming.HasValue && request.IsUpcoming.Value)
            {
                var upcomingDate = DateTime.UtcNow.AddDays(7);
                assignmentsList = assignmentsList.Where(a => a.DueDate >= DateTime.UtcNow && a.DueDate <= upcomingDate).ToList();
            }

            if (request.IsOverdue.HasValue && request.IsOverdue.Value)
            {
                assignmentsList = assignmentsList.Where(a => 
                    (a.ExtendedDueDate.HasValue ? a.ExtendedDueDate.Value : a.DueDate) < DateTime.UtcNow).ToList();
            }

            // Filter Draft assignments for non-Lecturers
            if (request.RequestUserRole != RoleConstants.Lecturer)
            {
                assignmentsList = assignmentsList.Where(a => a.Status != AssignmentStatus.Draft).ToList();
            }

            // Search
            if (!string.IsNullOrWhiteSpace(request.SearchQuery))
            {
                var searchLower = request.SearchQuery.ToLower();
                assignmentsList = assignmentsList.Where(a => 
                    a.Title.ToLower().Contains(searchLower) || 
                    (a.Description != null && a.Description.ToLower().Contains(searchLower))).ToList();
            }

            // Get total count before pagination
            var totalCount = assignmentsList.Count;

            // Sorting
            assignmentsList = request.SortBy.ToLower() switch
            {
                "title" => request.SortOrder.ToLower() == "desc" 
                    ? assignmentsList.OrderByDescending(a => a.Title).ToList()
                    : assignmentsList.OrderBy(a => a.Title).ToList(),
                "createdat" => request.SortOrder.ToLower() == "desc" 
                    ? assignmentsList.OrderByDescending(a => a.CreatedAt).ToList()
                    : assignmentsList.OrderBy(a => a.CreatedAt).ToList(),
                "status" => request.SortOrder.ToLower() == "desc" 
                    ? assignmentsList.OrderByDescending(a => a.Status).ToList()
                    : assignmentsList.OrderBy(a => a.Status).ToList(),
                _ => request.SortOrder.ToLower() == "desc" 
                    ? assignmentsList.OrderByDescending(a => a.DueDate).ToList()
                    : assignmentsList.OrderBy(a => a.DueDate).ToList()
            };

            // Pagination
            var pagedAssignments = assignmentsList
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var assignmentDtos = new List<AssignmentSummaryDto>();
            foreach (var a in pagedAssignments)
            {
                var effectiveDueDate = a.ExtendedDueDate ?? a.DueDate;
                var daysUntilDue = (int)(effectiveDueDate - DateTime.UtcNow).TotalDays;

                // Get all assigned group IDs if this is a group assignment
                List<Guid>? assignedGroupIds = a.IsGroupAssignment && a.AssignedGroups != null && a.AssignedGroups.Any()
                    ? a.AssignedGroups.Select(g => g.Id).ToList()
                    : null;

                // Deserialize attachments
                var attachments = AssignmentAttachmentsMetadata.FromJson(a.Attachments);

                // Use snapshot weight (captured at assignment creation time)
                decimal resolvedWeight = a.WeightPercentageSnapshot ?? 0;

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
                    StatusDisplay = a.Status.ToString(),
                    IsGroupAssignment = a.IsGroupAssignment,
                    MaxPoints = a.MaxPoints,
                    WeightPercentage = resolvedWeight,
                    GroupIds = assignedGroupIds,
                    Attachments = attachments.Files.Any() ? attachments.Files : null,
                    IsOverdue = effectiveDueDate < DateTime.UtcNow,
                    DaysUntilDue = daysUntilDue,
                    AssignedGroupsCount = a.AssignedGroups?.Count ?? 0,
                    CreatedAt = a.CreatedAt
                });
            }

            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            return new GetAssignmentsByTopicResponse
            {
                Success = true,
                Message = "Assignments retrieved successfully",
                Assignments = assignmentDtos,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = totalPages
            };
        }
        catch (Exception ex)
        {
            return new GetAssignmentsByTopicResponse
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
