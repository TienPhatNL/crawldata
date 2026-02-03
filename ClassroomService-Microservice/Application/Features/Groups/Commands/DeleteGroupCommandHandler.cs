using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.Groups.Commands;

public class DeleteGroupCommandHandler : IRequestHandler<DeleteGroupCommand, DeleteGroupResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DeleteGroupCommandHandler> _logger;

    public DeleteGroupCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<DeleteGroupCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<DeleteGroupResponse> Handle(DeleteGroupCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue)
            {
                return new DeleteGroupResponse
                {
                    Success = false,
                    Message = Messages.Error.UserIdNotFound
                };
            }

            // Get group with course and members
            var group = await _unitOfWork.Groups.GetAsync(
                g => g.Id == request.GroupId,
                cancellationToken,
                g => g.Course,
                g => g.Members);

            if (group == null)
            {
                return new DeleteGroupResponse
                {
                    Success = false,
                    Message = Messages.Error.GroupNotFound
                };
            }

            // Null check for Course
            if (group.Course == null)
            {
                _logger.LogError("Group {GroupId} has null Course navigation property", request.GroupId);
                return new DeleteGroupResponse
                {
                    Success = false,
                    Message = "Group data is incomplete"
                };
            }

            // Verify user is the lecturer
            if (group.Course.LecturerId != currentUserId.Value)
            {
                return new DeleteGroupResponse
                {
                    Success = false,
                    Message = Messages.Error.OnlyLecturerCanManageGroups
                };
            }

            // Check if group has been assigned to an assignment
            if (group.AssignmentId.HasValue)
            {
                return new DeleteGroupResponse
                {
                    Success = false,
                    Message = Messages.Error.GroupAssignedToAssignment
                };
            }

            _logger.LogInformation(Messages.Logging.GroupDeleting, request.GroupId);

            // Delete all group members first
            if (group.Members != null && group.Members.Any())
            {
                _logger.LogInformation("Deleting {MemberCount} members from group {GroupId}", group.Members.Count, request.GroupId);
                await _unitOfWork.GroupMembers.DeleteRangeAsync(group.Members);
            }

            // Delete the group
            await _unitOfWork.Groups.DeleteAsync(group);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Group {GroupId} and its members deleted successfully", request.GroupId);

            return new DeleteGroupResponse
            {
                Success = true,
                Message = Messages.Success.GroupDeleted
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting group {GroupId}", request.GroupId);
            return new DeleteGroupResponse
            {
                Success = false,
                Message = Messages.Helpers.FormatError(Messages.Error.GroupDeletionFailed, ex.Message)
            };
        }
    }
}
