using MediatR;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Application.Features.Assignments.DTOs;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Events;

namespace ClassroomService.Application.Features.Assignments.Commands;

public class ExtendDueDateCommandHandler : IRequestHandler<ExtendDueDateCommand, ExtendDueDateResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public ExtendDueDateCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ExtendDueDateResponse> Handle(ExtendDueDateCommand request, CancellationToken cancellationToken)
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
                return new ExtendDueDateResponse
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
                return new ExtendDueDateResponse
                {
                    Success = false,
                    Message = "Cannot extend due date for assignment with graded reports. Graded reports are permanently locked.",
                    Assignment = null
                };
            }

            // Validate extended date is after original due date
            if (request.ExtendedDueDate <= assignment.DueDate)
            {
                return new ExtendDueDateResponse
                {
                    Success = false,
                    Message = "Extended due date must be after the original due date",
                    Assignment = null
                };
            }

            // Update extended due date
            assignment.ExtendedDueDate = request.ExtendedDueDate;
            assignment.UpdatedAt = DateTime.UtcNow;

            // Update status if applicable
            var now = DateTime.UtcNow;
            if (assignment.Status == AssignmentStatus.Overdue && request.ExtendedDueDate > now)
            {
                assignment.Status = AssignmentStatus.Extended;
            }
            else if (assignment.DueDate < now && request.ExtendedDueDate >= now)
            {
                assignment.Status = AssignmentStatus.Extended;
            }

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

            // Add domain event
            assignment.AddDomainEvent(new AssignmentDueDateExtendedEvent(
                assignment.Id,
                assignment.CourseId,
                assignment.Title,
                assignment.DueDate,
                request.ExtendedDueDate,
                studentIds));

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

            return new ExtendDueDateResponse
            {
                Success = true,
                Message = "Due date extended successfully",
                Assignment = assignmentDto
            };
        }
        catch (Exception ex)
        {
            return new ExtendDueDateResponse
            {
                Success = false,
                Message = $"Error extending due date: {ex.Message}",
                Assignment = null
            };
        }
    }
}
