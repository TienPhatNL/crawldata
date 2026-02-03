using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Events;
using ClassroomService.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClassroomService.Application.Features.GroupMembers.Commands;

public class UnassignGroupLeaderCommandHandler : IRequestHandler<UnassignGroupLeaderCommand, UnassignGroupLeaderResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IKafkaUserService _userService;
    private readonly ILogger<UnassignGroupLeaderCommandHandler> _logger;

    public UnassignGroupLeaderCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IKafkaUserService userService,
        ILogger<UnassignGroupLeaderCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _userService = userService;
        _logger = logger;
    }

    public async Task<UnassignGroupLeaderResponse> Handle(UnassignGroupLeaderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue)
            {
                return new UnassignGroupLeaderResponse
                {
                    Success = false,
                    Message = Messages.Error.UserIdNotFound
                };
            }

            // Use GetGroupWithMembersAsync to properly load all navigation properties
            var group = await _unitOfWork.Groups.GetGroupWithMembersAsync(request.GroupId, cancellationToken);

            if (group == null)
            {
                return new UnassignGroupLeaderResponse
                {
                    Success = false,
                    Message = Messages.Error.GroupNotFound
                };
            }

            // Null check for Course
            if (group.Course == null)
            {
                _logger.LogError("Group {GroupId} has null Course navigation property", request.GroupId);
                return new UnassignGroupLeaderResponse
                {
                    Success = false,
                    Message = "Group data is incomplete"
                };
            }

            // Verify user is the lecturer
            if (group.Course.LecturerId != currentUserId.Value)
            {
                return new UnassignGroupLeaderResponse
                {
                    Success = false,
                    Message = Messages.Error.OnlyLecturerCanManageGroups
                };
            }

            // Check if group is locked
            if (group.IsLocked)
            {
                return new UnassignGroupLeaderResponse
                {
                    Success = false,
                    Message = Messages.Error.GroupLocked
                };
            }

            // Find current leader
            var currentLeader = group.Members?.FirstOrDefault(m => m.IsLeader);
            
            if (currentLeader == null)
            {
                return new UnassignGroupLeaderResponse
                {
                    Success = false,
                    Message = "No leader is currently assigned to this group"
                };
            }

            _logger.LogInformation("Unassigning leader from group {GroupId}", request.GroupId);

            var previousLeaderId = currentLeader.Enrollment.StudentId;
            
            // Get user information for response
            var users = await _userService.GetUsersByIdsAsync(new List<Guid> { previousLeaderId }, cancellationToken);
            var previousLeaderUser = users.FirstOrDefault();
            var previousLeaderName = previousLeaderUser != null 
                ? $"{previousLeaderUser.LastName} {previousLeaderUser.FirstName}".Trim() 
                : "Unknown";

            // Remove leader status
            currentLeader.IsLeader = false;
            currentLeader.Role = GroupMemberRole.Member;

            // Get all group member IDs
            var groupMemberIds = (await _unitOfWork.GroupMembers
                .GetManyAsync(gm => gm.GroupId == request.GroupId, cancellationToken))
                .Select(gm => gm.Enrollment.StudentId)
                .ToList();

            // Add domain event (using Guid.Empty to indicate no new leader)
            group.AddDomainEvent(new GroupLeaderChangedEvent(
                group.Id,
                group.CourseId,
                previousLeaderId,
                Guid.Empty, // No new leader assigned
                currentUserId.Value,
                groupMemberIds,
                group.Name
            ));

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Leader unassigned from group {GroupId}", request.GroupId);

            return new UnassignGroupLeaderResponse
            {
                Success = true,
                Message = "Group leader unassigned successfully",
                GroupId = group.Id,
                PreviousLeaderId = previousLeaderId,
                PreviousLeaderName = previousLeaderName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unassigning leader from group {GroupId}", request.GroupId);
            return new UnassignGroupLeaderResponse
            {
                Success = false,
                Message = Messages.Helpers.FormatError("Failed to unassign group leader", ex.Message)
            };
        }
    }
}
