using ClassroomService.Domain.Events;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Enums;
using MediatR;

namespace ClassroomService.Application.Features.Assignments.Commands;

public class UnassignGroupsCommandHandler : IRequestHandler<UnassignGroupsCommand, UnassignGroupsResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public UnassignGroupsCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UnassignGroupsResponse> Handle(UnassignGroupsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var assignment = await _unitOfWork.Assignments
                .GetAsync(a => a.Id == request.AssignmentId, cancellationToken);

            if (assignment == null)
            {
                return new UnassignGroupsResponse
                {
                    Success = false,
                    Message = "Assignment not found",
                    UnassignedCount = 0
                };
            }

            // Check if any reports have been graded for this assignment
            var gradedReports = await _unitOfWork.Reports
                .GetManyAsync(r => r.AssignmentId == assignment.Id && r.Status == ReportStatus.Graded, cancellationToken);
            
            if (gradedReports.Any())
            {
                return new UnassignGroupsResponse
                {
                    Success = false,
                    Message = "Cannot unassign groups from an assignment that has graded reports",
                    UnassignedCount = 0
                };
            }

            // Get groups assigned to this assignment
            var groups = await _unitOfWork.Groups
                .GetManyAsync(g => request.GroupIds.Contains(g.Id) && g.AssignmentId == request.AssignmentId, cancellationToken);

            if (groups.Count() == 0)
            {
                return new UnassignGroupsResponse
                {
                    Success = false,
                    Message = "No groups found assigned to this assignment",
                    UnassignedCount = 0
                };
            }

            // Collect all group member IDs for bulk notification
            var allGroupMemberIds = new List<Guid>();

            // Unassign groups and notify members
            var groupIds = groups.Select(g => g.Id).ToList();
            foreach (var group in groups)
            {
                // Get group members with enrollment loaded
                var groupMembers = await _unitOfWork.GroupMembers
                    .GetMembersByGroupAsync(group.Id, cancellationToken);
                
                var groupMemberIds = groupMembers
                    .Select(gm => gm.StudentId)
                    .ToList();

                allGroupMemberIds.AddRange(groupMemberIds);
                
                // Add domain event for each group unassignment to notify members
                group.AddDomainEvent(new GroupAssignmentUnassignedEvent(
                    group.Id,
                    assignment.Id,
                    assignment.CourseId,
                    request.UnassignedBy ?? Guid.Empty,
                    groupMemberIds,
                    group.Name,
                    assignment.Title
                ));
                
                group.AssignmentId = null;
            }

            // Add domain event for bulk unassignment tracking (with all unique member IDs)
            assignment.AddDomainEvent(new GroupsUnassignedFromAssignmentEvent(
                assignment.Id,
                assignment.CourseId,
                assignment.Title,
                groupIds,
                allGroupMemberIds.Distinct().ToList()));

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new UnassignGroupsResponse
            {
                Success = true,
                Message = $"Successfully unassigned {groups.Count()} group(s) from assignment",
                UnassignedCount = groups.Count()
            };
        }
        catch (Exception ex)
        {
            return new UnassignGroupsResponse
            {
                Success = false,
                Message = $"Error unassigning groups: {ex.Message}",
                UnassignedCount = 0
            };
        }
    }
}
