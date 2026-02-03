using MediatR;
using Microsoft.Extensions.Logging;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.DTOs;
using ClassroomService.Application.Common.Interfaces;

namespace ClassroomService.Application.Features.Groups.Commands;

public class UpdateGroupCommandHandler : IRequestHandler<UpdateGroupCommand, UpdateGroupResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateGroupCommandHandler> _logger;

    public UpdateGroupCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<UpdateGroupCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<UpdateGroupResponse> Handle(UpdateGroupCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue)
            {
                return new UpdateGroupResponse
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
                return new UpdateGroupResponse
                {
                    Success = false,
                    Message = Messages.Error.GroupNotFound
                };
            }

            // Null check for Course
            if (group.Course == null)
            {
                _logger.LogError("Group {GroupId} has null Course navigation property", request.GroupId);
                return new UpdateGroupResponse
                {
                    Success = false,
                    Message = "Group data is incomplete"
                };
            }

            // Verify user is the lecturer
            if (group.Course.LecturerId != currentUserId.Value)
            {
                return new UpdateGroupResponse
                {
                    Success = false,
                    Message = Messages.Error.OnlyLecturerCanManageGroups
                };
            }

            // Check if name already exists in course (excluding current group)
            if (group.Name != request.Name)
            {
                var nameExists = await _unitOfWork.Groups
                    .ExistsAsync(g => g.CourseId == group.CourseId && g.Name == request.Name && g.Id != group.Id, cancellationToken);

                if (nameExists)
                {
                    return new UpdateGroupResponse
                    {
                        Success = false,
                        Message = Messages.Error.GroupNameExists
                    };
                }
            }

            // Check if reducing max members below current count
            if (request.MaxMembers.HasValue && group.Members != null && group.Members.Count > request.MaxMembers.Value)
            {
                return new UpdateGroupResponse
                {
                    Success = false,
                    Message = $"Cannot set max members to {request.MaxMembers} as the group currently has {group.Members.Count} members"
                };
            }

            _logger.LogInformation(Messages.Logging.GroupUpdating, request.GroupId);

            // Update group
            group.Name = request.Name;
            group.Description = request.Description;
            group.MaxMembers = request.MaxMembers;
            group.IsLocked = request.IsLocked;
            group.LastModifiedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Build response DTO
            var groupDto = new GroupDto
            {
                Id = group.Id,
                CourseId = group.CourseId,
                CourseName = group.Course?.Name ?? "Unknown Course",
                Name = group.Name,
                Description = group.Description ?? string.Empty,
                MaxMembers = group.MaxMembers,
                IsLocked = group.IsLocked,
                AssignmentId = group.AssignmentId,
                MemberCount = group.Members?.Count ?? 0,
                CreatedAt = group.CreatedAt,
                CreatedBy = group.CreatedBy
            };

            return new UpdateGroupResponse
            {
                Success = true,
                Message = Messages.Success.GroupUpdated,
                Group = groupDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating group {GroupId}", request.GroupId);
            return new UpdateGroupResponse
            {
                Success = false,
                Message = Messages.Helpers.FormatError(Messages.Error.GroupUpdateFailed, ex.Message)
            };
        }
    }
}
