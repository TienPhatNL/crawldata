using MediatR;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Application.Features.Assignments.DTOs;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Events;

namespace ClassroomService.Application.Features.Assignments.Commands;

public class CloseAssignmentCommandHandler : IRequestHandler<CloseAssignmentCommand, CloseAssignmentResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public CloseAssignmentCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CloseAssignmentResponse> Handle(CloseAssignmentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var assignment = await _unitOfWork.Assignments
                .GetAsync(
                    a => a.Id == request.AssignmentId, 
                    cancellationToken,
                    a => a.Course,
                    a => a.AssignedGroups);

            if (assignment == null)
            {
                return new CloseAssignmentResponse
                {
                    Success = false,
                    Message = "Assignment not found",
                    Assignment = null
                };
            }

            // Check if any reports have been graded for this assignment
            var gradedReports = await _unitOfWork.Reports
                .GetManyAsync(r => r.AssignmentId == assignment.Id && r.Status == ReportStatus.Graded, cancellationToken);
            
            if (gradedReports.Any())
            {
                return new CloseAssignmentResponse
                {
                    Success = false,
                    Message = "Cannot close assignment with graded reports. Graded reports are in a permanent state.",
                    Assignment = null
                };
            }

            // Check if assignment is in Draft status
            if (assignment.Status == AssignmentStatus.Draft)
            {
                return new CloseAssignmentResponse
                {
                    Success = false,
                    Message = "Cannot close an assignment in Draft status",
                    Assignment = null
                };
            }

            // Check if assignment is already closed
            if (assignment.Status == AssignmentStatus.Closed)
            {
                return new CloseAssignmentResponse
                {
                    Success = false,
                    Message = "Assignment is already closed",
                    Assignment = null
                };
            }

            // Close the assignment
            var oldStatus = assignment.Status;
            assignment.Status = AssignmentStatus.Closed;
            assignment.UpdatedAt = DateTime.UtcNow;

            // Get student IDs from assigned groups (not all enrolled students)
            var assignedGroups = await _unitOfWork.Groups
                .GetManyAsync(g => g.AssignmentId == assignment.Id, cancellationToken);
            
            var groupMemberIds = new List<Guid>();
            foreach (var group in assignedGroups)
            {
                var members = await _unitOfWork.GroupMembers
                    .GetMembersByGroupAsync(group.Id, cancellationToken);
                groupMemberIds.AddRange(members.Select(m => m.StudentId));
            }
            
            var studentIds = groupMemberIds.Distinct().ToList();

            // Get course to get lecturer ID
            var course = assignment.Course ?? await _unitOfWork.Courses
                .GetAsync(c => c.Id == assignment.CourseId, cancellationToken);

            // Add domain events
            assignment.AddDomainEvent(new AssignmentClosedEvent(
                assignment.Id,
                assignment.CourseId,
                assignment.Title,
                DateTime.UtcNow,
                studentIds));

            assignment.AddDomainEvent(new AssignmentStatusChangedEvent(
                assignment.Id,
                assignment.CourseId,
                assignment.Title,
                oldStatus,
                AssignmentStatus.Closed,
                course?.LecturerId ?? Guid.Empty,
                isAutomatic: false));

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var effectiveDueDate = assignment.ExtendedDueDate ?? assignment.DueDate;
            var daysUntilDue = (int)(effectiveDueDate - DateTime.UtcNow).TotalDays;

            var assignmentDto = new AssignmentDetailDto
            {
                Id = assignment.Id,
                CourseId = assignment.CourseId,
                CourseName = assignment.Course?.Name ?? "Unknown Course",
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

            return new CloseAssignmentResponse
            {
                Success = true,
                Message = "Assignment closed successfully",
                Assignment = assignmentDto
            };
        }
        catch (Exception ex)
        {
            return new CloseAssignmentResponse
            {
                Success = false,
                Message = $"Error closing assignment: {ex.Message}",
                Assignment = null
            };
        }
    }
}
