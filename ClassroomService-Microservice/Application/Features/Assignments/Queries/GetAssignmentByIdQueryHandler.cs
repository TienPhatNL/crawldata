using MediatR;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Application.Features.Assignments.DTOs;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Application.Features.Assignments.Queries;

public class GetAssignmentByIdQueryHandler : IRequestHandler<GetAssignmentByIdQuery, GetAssignmentByIdResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetAssignmentByIdQueryHandler> _logger;

    public GetAssignmentByIdQueryHandler(
        IUnitOfWork unitOfWork, 
        ILogger<GetAssignmentByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GetAssignmentByIdResponse> Handle(GetAssignmentByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var assignment = await _unitOfWork.Assignments
                .GetAsync(
                    a => a.Id == request.AssignmentId,
                    cancellationToken,
                    a => a.Course,
                    a => a.Course.CourseCode,
                    a => a.Topic,
                    a => a.AssignedGroups);

            if (assignment == null)
            {
                return new GetAssignmentByIdResponse
                {
                    Success = false,
                    Message = "Assignment not found",
                    Assignment = null
                };
            }

            // Hide Draft assignments from non-Lecturers
            if (assignment.Status == AssignmentStatus.Draft && 
                request.RequestUserRole != RoleConstants.Lecturer)
            {
                return new GetAssignmentByIdResponse
                {
                    Success = false,
                    Message = "Assignment not found",
                    Assignment = null
                };
            }

            var effectiveDueDate = assignment.ExtendedDueDate ?? assignment.DueDate;
            var daysUntilDue = (int)(effectiveDueDate - DateTime.UtcNow).TotalDays;

            // Get all assigned group IDs if this is a group assignment
            List<Guid>? assignedGroupIds = assignment.IsGroupAssignment && assignment.AssignedGroups != null && assignment.AssignedGroups.Any()
                ? assignment.AssignedGroups.Select(g => g.Id).ToList()
                : null;

            // Deserialize attachments
            var attachments = AssignmentAttachmentsMetadata.FromJson(assignment.Attachments);

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
                GroupIds = assignedGroupIds,
                IsOverdue = effectiveDueDate < DateTime.UtcNow,
                DaysUntilDue = daysUntilDue,
                AssignedGroupsCount = assignment.AssignedGroups?.Count ?? 0,
                Attachments = attachments.Files.Any() ? attachments.Files : null,
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

            return new GetAssignmentByIdResponse
            {
                Success = true,
                Message = "Assignment retrieved successfully",
                Assignment = assignmentDto
            };
        }
        catch (Exception ex)
        {
            return new GetAssignmentByIdResponse
            {
                Success = false,
                Message = $"Error retrieving assignment: {ex.Message}",
                Assignment = null
            };
        }
    }
}
