using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Events;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.GroupMembers.Commands;

public class AssignGroupLeaderCommandHandler : IRequestHandler<AssignGroupLeaderCommand, AssignGroupLeaderResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IKafkaUserService _userService;
    private readonly ILogger<AssignGroupLeaderCommandHandler> _logger;

    public AssignGroupLeaderCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IKafkaUserService userService,
        ILogger<AssignGroupLeaderCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _userService = userService;
        _logger = logger;
    }

    public async Task<AssignGroupLeaderResponse> Handle(AssignGroupLeaderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue)
            {
                return new AssignGroupLeaderResponse
                {
                    Success = false,
                    Message = Messages.Error.UserIdNotFound
                };
            }

            // Use GetGroupWithMembersAsync to properly load all navigation properties
            var group = await _unitOfWork.Groups.GetGroupWithMembersAsync(request.GroupId, cancellationToken);

            if (group == null)
            {
                return new AssignGroupLeaderResponse
                {
                    Success = false,
                    Message = Messages.Error.GroupNotFound
                };
            }

            // Null check for Course
            if (group.Course == null)
            {
                _logger.LogError("Group {GroupId} has null Course navigation property", request.GroupId);
                return new AssignGroupLeaderResponse
                {
                    Success = false,
                    Message = "Group data is incomplete"
                };
            }

            // Verify user is the lecturer
            if (group.Course.LecturerId != currentUserId.Value)
            {
                return new AssignGroupLeaderResponse
                {
                    Success = false,
                    Message = Messages.Error.OnlyLecturerCanManageGroups
                };
            }

            // Check if group is locked
            if (group.IsLocked)
            {
                return new AssignGroupLeaderResponse
                {
                    Success = false,
                    Message = Messages.Error.GroupLocked
                };
            }

            // Find the new leader member
            var newLeaderMember = group.Members?.FirstOrDefault(m => m.Enrollment.StudentId == request.StudentId);
            if (newLeaderMember == null)
            {
                return new AssignGroupLeaderResponse
                {
                    Success = false,
                    Message = Messages.Error.NewLeaderNotInGroup
                };
            }

            // Check if already leader
            if (newLeaderMember.IsLeader)
            {
                return new AssignGroupLeaderResponse
                {
                    Success = true,
                    Message = "Student is already the group leader",
                    GroupId = group.Id,
                    NewLeaderId = request.StudentId
                };
            }

            _logger.LogInformation(Messages.Logging.AssigningLeader, request.StudentId, request.GroupId);

            // Find current leader (if any)
            var currentLeader = group.Members?.FirstOrDefault(m => m.IsLeader);
            Guid? previousLeaderId = currentLeader?.Enrollment?.StudentId;
            string? previousLeaderName = null;

            // Get user information for response
            var userIds = new List<Guid> { request.StudentId };
            if (previousLeaderId.HasValue)
            {
                userIds.Add(previousLeaderId.Value);
            }

            var users = await _userService.GetUsersByIdsAsync(userIds, cancellationToken);
            var userDict = users.ToDictionary(u => u.Id);

            var newLeaderUser = userDict.ContainsKey(request.StudentId) ? userDict[request.StudentId] : null;
            var newLeaderName = newLeaderUser != null ? $"{newLeaderUser.LastName} {newLeaderUser.FirstName}".Trim() : "Unknown";

            // Remove leader status from current leader
            if (currentLeader != null)
            {
                currentLeader.IsLeader = false;
                currentLeader.Role = GroupMemberRole.Member;
                
                var previousLeaderUser = userDict.ContainsKey(previousLeaderId!.Value) ? userDict[previousLeaderId.Value] : null;
                previousLeaderName = previousLeaderUser != null ? $"{previousLeaderUser.LastName} {previousLeaderUser.FirstName}".Trim() : "Unknown";
            }

            // Assign leader status to new leader
            newLeaderMember.IsLeader = true;
            newLeaderMember.Role = GroupMemberRole.Leader;

            // Get all group member IDs
            var groupMemberIds = (await _unitOfWork.GroupMembers
                .GetManyAsync(gm => gm.GroupId == request.GroupId, cancellationToken))
                .Select(gm => gm.Enrollment.StudentId)
                .ToList();

            // Add domain event
            group.AddDomainEvent(new GroupLeaderChangedEvent(
                group.Id,
                group.CourseId,
                previousLeaderId,
                request.StudentId,
                currentUserId.Value,
                groupMemberIds,
                group.Name
            ));

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(Messages.Logging.LeaderSet, request.StudentId, request.GroupId);

            return new AssignGroupLeaderResponse
            {
                Success = true,
                Message = Messages.Success.LeaderAssigned,
                GroupId = group.Id,
                NewLeaderId = request.StudentId,
                NewLeaderName = newLeaderName,
                PreviousLeaderId = previousLeaderId,
                PreviousLeaderName = previousLeaderName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning leader {StudentId} to group {GroupId}", request.StudentId, request.GroupId);
            return new AssignGroupLeaderResponse
            {
                Success = false,
                Message = Messages.Helpers.FormatError(Messages.Error.LeaderSetFailed, ex.Message)
            };
        }
    }
}
