using MediatR;
using Microsoft.EntityFrameworkCore;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Events;
using ClassroomService.Domain.Enums;
using ClassroomService.Application.Common.Interfaces;

namespace ClassroomService.Application.Features.Assignments.Commands;

public class AssignGroupsCommandHandler : IRequestHandler<AssignGroupsCommand, AssignGroupsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;
    private readonly ICurrentUserService _currentUserService;

    public AssignGroupsCommandHandler(IUnitOfWork unitOfWork, IKafkaUserService userService, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
        _currentUserService = currentUserService;
    }

    public async Task<AssignGroupsResponse> Handle(AssignGroupsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue)
            {
                return new AssignGroupsResponse
                {
                    Success = false,
                    Message = "User not authenticated",
                    AssignedCount = 0,
                    Groups = new List<GroupDto>()
                };
            }

            var assignment = await _unitOfWork.Assignments
                .GetAsync(a => a.Id == request.AssignmentId, cancellationToken, a => a.Course);

            if (assignment == null)
            {
                return new AssignGroupsResponse
                {
                    Success = false,
                    Message = "Assignment not found",
                    AssignedCount = 0,
                    Groups = new List<GroupDto>()
                };
            }

            // Only allow assigning groups to Scheduled or Active assignments
            if (assignment.Status != AssignmentStatus.Scheduled && assignment.Status != AssignmentStatus.Active)
            {
                return new AssignGroupsResponse
                {
                    Success = false,
                    Message = $"Cannot assign groups to assignment with status '{assignment.Status}'. Assignment must be Scheduled or Active.",
                    AssignedCount = 0,
                    Groups = new List<GroupDto>()
                };
            }

            // Validate that assignment is a group assignment
            if (!assignment.IsGroupAssignment)
            {
                return new AssignGroupsResponse
                {
                    Success = false,
                    Message = "Cannot assign groups to an individual assignment. Assignment must be marked as IsGroupAssignment.",
                    AssignedCount = 0,
                    Groups = new List<GroupDto>()
                };
            }

            // Get all groups with members for the course
            var allGroups = await _unitOfWork.Groups
                .GetGroupsByCourseAsync(assignment.CourseId, cancellationToken);
            
            var groups = allGroups.Where(g => request.GroupIds.Contains(g.Id)).ToList();

            if (groups.Count != request.GroupIds.Count)
            {
                return new AssignGroupsResponse
                {
                    Success = false,
                    Message = "One or more groups not found",
                    AssignedCount = 0,
                    Groups = new List<GroupDto>()
                };
            }

            // Validate all groups belong to same course
            if (groups.Any(g => g.CourseId != assignment.CourseId))
            {
                return new AssignGroupsResponse
                {
                    Success = false,
                    Message = "All groups must belong to the same course as the assignment",
                    AssignedCount = 0,
                    Groups = new List<GroupDto>()
                };
            }

            // Check if any group already has an assignment
            var groupsWithAssignments = groups.Where(g => g.AssignmentId.HasValue).ToList();
            if (groupsWithAssignments.Any())
            {
                var groupNames = string.Join(", ", groupsWithAssignments.Select(g => g.Name));
                return new AssignGroupsResponse
                {
                    Success = false,
                    Message = $"The following groups already have assignments: {groupNames}",
                    AssignedCount = 0,
                    Groups = new List<GroupDto>()
                };
            }

            // Collect all group member IDs for bulk notification
            var allGroupMemberIds = new List<Guid>();

            // Assign groups to assignment
            foreach (var group in groups)
            {
                group.AssignmentId = assignment.Id;
                
                // Get group member IDs
                var groupMemberIds = (await _unitOfWork.GroupMembers
                    .GetManyAsync(gm => gm.GroupId == group.Id, cancellationToken))
                    .Select(gm => gm.Enrollment.StudentId)
                    .ToList();

                allGroupMemberIds.AddRange(groupMemberIds);

                // Raise domain event for each group assignment
                assignment.AddDomainEvent(new GroupAssignmentAssignedEvent(
                    group.Id,
                    assignment.Id,
                    assignment.CourseId,
                    currentUserId.Value,
                    groupMemberIds,
                    group.Name,
                    assignment.Title,
                    (int)assignment.Status
                ));
                
                await _unitOfWork.Groups.UpdateAsync(group, cancellationToken);
            }

            // Add domain event for bulk assignment tracking (with all unique member IDs)
            assignment.AddDomainEvent(new GroupsAssignedToAssignmentEvent(
                assignment.Id,
                assignment.CourseId,
                assignment.Title,
                request.GroupIds,
                allGroupMemberIds.Distinct().ToList()));

            await _unitOfWork.SaveChangesAsync(cancellationToken);            // Build response DTOs with leader information
            var groupDtos = new List<GroupDto>();
            foreach (var g in groups)
            {
                // Find the leader
                var leader = g.Members?.FirstOrDefault(m => m.Role == GroupMemberRole.Leader);
                string? leaderName = null;
                Guid? leaderId = null;

                if (leader != null)
                {
                    leaderId = leader.StudentId;
                    var leaderInfo = await _userService.GetUserByIdAsync(leader.StudentId, cancellationToken);
                    leaderName = leaderInfo != null ? $"{leaderInfo.FirstName} {leaderInfo.LastName}" : null;
                }

                groupDtos.Add(new GroupDto
                {
                    Id = g.Id,
                    CourseId = g.CourseId,
                    CourseName = g.Course?.Name ?? assignment.Course?.Name ?? "Unknown Course",
                    Name = g.Name,
                    Description = g.Description ?? string.Empty,
                    MaxMembers = g.MaxMembers,
                    IsLocked = g.IsLocked,
                    AssignmentId = g.AssignmentId,
                    AssignmentTitle = assignment.Title,
                    MemberCount = g.Members?.Count ?? 0,
                    LeaderId = leaderId,
                    LeaderName = leaderName,
                    CreatedAt = g.CreatedAt
                });
            }

            return new AssignGroupsResponse
            {
                Success = true,
                Message = $"Successfully assigned {groups.Count} group(s) to assignment",
                AssignedCount = groups.Count,
                Groups = groupDtos
            };
        }
        catch (Exception ex)
        {
            return new AssignGroupsResponse
            {
                Success = false,
                Message = $"Error assigning groups: {ex.Message}",
                AssignedCount = 0,
                Groups = new List<GroupDto>()
            };
        }
    }
}

