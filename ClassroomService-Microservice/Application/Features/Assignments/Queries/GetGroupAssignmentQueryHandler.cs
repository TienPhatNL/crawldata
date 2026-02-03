using MediatR;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Application.Features.Assignments.DTOs;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Application.Features.Assignments.Queries;

public class GetGroupAssignmentQueryHandler : IRequestHandler<GetGroupAssignmentQuery, GetGroupAssignmentResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetGroupAssignmentQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GetGroupAssignmentResponse> Handle(GetGroupAssignmentQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var group = await _unitOfWork.Groups.GetGroupWithMembersAsync(request.GroupId, cancellationToken);

            if (group == null)
            {
                return new GetGroupAssignmentResponse
                {
                    Success = false,
                    Message = "Group not found",
                    GroupId = request.GroupId,
                    GroupName = string.Empty,
                    Assignment = null,
                    HasAssignment = false
                };
            }

            if (group.Assignment == null)
            {
                return new GetGroupAssignmentResponse
                {
                    Success = true,
                    Message = "Group has no assignment assigned",
                    GroupId = request.GroupId,
                    GroupName = group.Name,
                    Assignment = null,
                    HasAssignment = false
                };
            }

            var assignment = group.Assignment;

            // Hide Draft assignments from non-Lecturers
            if (assignment.Status == AssignmentStatus.Draft && 
                request.RequestUserRole != RoleConstants.Lecturer)
            {
                return new GetGroupAssignmentResponse
                {
                    Success = true,
                    Message = "Group has no assignment assigned",
                    GroupId = request.GroupId,
                    GroupName = group.Name,
                    Assignment = null,
                    HasAssignment = false
                };
            }
            var effectiveDueDate = assignment.ExtendedDueDate ?? assignment.DueDate;
            var daysUntilDue = (int)(effectiveDueDate - DateTime.UtcNow).TotalDays;

            // Use snapshot weight (captured at assignment creation time)
            decimal resolvedWeight = assignment.WeightPercentageSnapshot ?? 0;

            var assignmentDto = new AssignmentDetailDto
            {
                Id = assignment.Id,
                CourseId = assignment.CourseId,
                CourseName = assignment.Course?.Name ?? "Unknown Course",
                TopicId = assignment.TopicId,
                TopicName = assignment.Topic?.Name ?? "Unknown Topic",
                Title = assignment.Title,
                Description = assignment.Description ?? string.Empty,
                StartDate = assignment.StartDate,
                DueDate = assignment.DueDate,
                ExtendedDueDate = assignment.ExtendedDueDate,
                Format = assignment.Format ?? string.Empty,
                Status = assignment.Status,
                StatusDisplay = assignment.Status.ToString(),
                IsGroupAssignment = assignment.IsGroupAssignment,
                MaxPoints = assignment.MaxPoints,
                WeightPercentage = resolvedWeight,
                IsOverdue = effectiveDueDate < DateTime.UtcNow,
                DaysUntilDue = daysUntilDue,
                AssignedGroupsCount = assignment.AssignedGroups?.Count ?? 0,
                CreatedAt = assignment.CreatedAt,
                UpdatedAt = assignment.UpdatedAt,
                AssignedGroups = assignment.AssignedGroups?.Select(g => new GroupDto
                {
                    Id = g.Id,
                    CourseId = g.CourseId,
                    Name = g.Name,
                    Description = g.Description ?? string.Empty,
                    MaxMembers = g.MaxMembers,
                    IsLocked = g.IsLocked,
                    AssignmentId = g.AssignmentId,
                    MemberCount = g.Members?.Count ?? 0,
                    CreatedAt = g.CreatedAt
                }).ToList() ?? new List<GroupDto>()
            };

            return new GetGroupAssignmentResponse
            {
                Success = true,
                Message = "Assignment retrieved successfully",
                GroupId = request.GroupId,
                GroupName = group.Name,
                Assignment = assignmentDto,
                HasAssignment = true
            };
        }
        catch (Exception ex)
        {
            return new GetGroupAssignmentResponse
            {
                Success = false,
                Message = $"Error retrieving group assignment: {ex.Message}",
                GroupId = request.GroupId,
                GroupName = string.Empty,
                Assignment = null,
                HasAssignment = false
            };
        }
    }
}
