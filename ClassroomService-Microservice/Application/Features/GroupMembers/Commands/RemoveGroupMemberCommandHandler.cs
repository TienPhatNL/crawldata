using MediatR;
using Microsoft.Extensions.Logging;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.Events;
using ClassroomService.Application.Common.Interfaces;

namespace ClassroomService.Application.Features.GroupMembers.Commands;

public class RemoveGroupMemberCommandHandler : IRequestHandler<RemoveGroupMemberCommand, RemoveGroupMemberResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<RemoveGroupMemberCommandHandler> _logger;

    public RemoveGroupMemberCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<RemoveGroupMemberCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<RemoveGroupMemberResponse> Handle(RemoveGroupMemberCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue)
            {
                return new RemoveGroupMemberResponse
                {
                    Success = false,
                    Message = Messages.Error.UserIdNotFound
                };
            }

            // Get group with course and members
            var group = await _unitOfWork.Groups.GetGroupWithMembersAsync(request.GroupId, cancellationToken);

            if (group == null)
            {
                return new RemoveGroupMemberResponse
                {
                    Success = false,
                    Message = Messages.Error.GroupNotFound
                };
            }

            // Null check for Course
            if (group.Course == null)
            {
                _logger.LogError("Group {GroupId} has null Course navigation property", request.GroupId);
                return new RemoveGroupMemberResponse
                {
                    Success = false,
                    Message = "Group data is incomplete"
                };
            }

            // Verify user is the lecturer
            if (group.Course.LecturerId != currentUserId.Value)
            {
                return new RemoveGroupMemberResponse
                {
                    Success = false,
                    Message = Messages.Error.OnlyLecturerCanManageGroups
                };
            }

            // Check if group is locked
            if (group.IsLocked)
            {
                return new RemoveGroupMemberResponse
                {
                    Success = false,
                    Message = Messages.Error.GroupLocked
                };
            }

            // Find the member (need to include enrollment to access StudentId)
            var member = await _unitOfWork.GroupMembers
                .GetGroupMemberAsync(request.GroupId, request.StudentId, cancellationToken);

            if (member == null)
            {
                return new RemoveGroupMemberResponse
                {
                    Success = false,
                    Message = Messages.Error.MemberNotInGroup
                };
            }

            _logger.LogInformation(Messages.Logging.MemberRemoving, request.StudentId, request.GroupId);

            var wasLeader = member.IsLeader;
            var studentId = member.Enrollment.StudentId;

            // Get remaining member IDs (excluding the one being removed)
            var remainingMemberIds = (await _unitOfWork.GroupMembers
                .GetManyAsync(gm => gm.GroupId == request.GroupId && gm.Enrollment.StudentId != studentId, cancellationToken))
                .Select(gm => gm.Enrollment.StudentId)
                .ToList();

            // Add domain event before removal
            member.AddDomainEvent(new GroupMemberRemovedEvent(
                member.GroupId,
                member.EnrollmentId,
                studentId,
                group.CourseId,
                wasLeader,
                currentUserId.Value,
                remainingMemberIds,
                group.Name
            ));

            await _unitOfWork.GroupMembers.DeleteAsync(member);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(Messages.Logging.MemberRemoved, request.StudentId, request.GroupId);

            return new RemoveGroupMemberResponse
            {
                Success = true,
                Message = wasLeader 
                    ? "Group leader removed successfully. Please assign a new leader."
                    : Messages.Success.MemberRemoved
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing member {StudentId} from group {GroupId}", 
                request.StudentId, request.GroupId);
            return new RemoveGroupMemberResponse
            {
                Success = false,
                Message = Messages.Helpers.FormatError(Messages.Error.MemberRemoveFailed, ex.Message)
            };
        }
    }
}
