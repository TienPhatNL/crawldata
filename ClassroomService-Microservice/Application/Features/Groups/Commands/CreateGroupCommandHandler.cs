using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Events;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.Groups.Commands;

/// <summary>
/// Handler for creating a new group
/// </summary>
public class CreateGroupCommandHandler : IRequestHandler<CreateGroupCommand, CreateGroupResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreateGroupCommandHandler> _logger;

    public CreateGroupCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<CreateGroupCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<CreateGroupResponse> Handle(CreateGroupCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get current user ID
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue)
            {
                return new CreateGroupResponse
                {
                    Success = false,
                    Message = Messages.Error.UserIdNotFound
                };
            }

            // Verify course exists
            var course = await _unitOfWork.Courses.GetByIdAsync(request.CourseId, cancellationToken);

            if (course == null)
            {
                return new CreateGroupResponse
                {
                    Success = false,
                    Message = Messages.Error.CourseNotFound
                };
            }

            // Verify user is the lecturer of the course
            if (course.LecturerId != currentUserId.Value)
            {
                return new CreateGroupResponse
                {
                    Success = false,
                    Message = Messages.Error.OnlyLecturerCanManageGroups
                };
            }

            // Check if group name already exists in this course
            var existingGroup = await _unitOfWork.Groups
                .ExistsAsync(g => g.CourseId == request.CourseId && g.Name == request.Name, cancellationToken);

            if (existingGroup)
            {
                return new CreateGroupResponse
                {
                    Success = false,
                    Message = Messages.Error.GroupNameExists
                };
            }

            _logger.LogInformation(Messages.Logging.GroupCreating, request.Name, request.CourseId);

            // Create group with IsLocked set to false by default
            var group = new Group
            {
                Id = Guid.NewGuid(),
                CourseId = request.CourseId,
                Name = request.Name,
                Description = request.Description,
                MaxMembers = request.MaxMembers,
                IsLocked = false, // Set to false by default
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUserId.Value
            };

            // Add domain event (initially no members)
            group.AddDomainEvent(new GroupCreatedEvent(
                group.Id,
                group.CourseId,
                group.Name,
                currentUserId.Value,
                new List<Guid>() // Empty list since group just created
            ));

            await _unitOfWork.Groups.AddAsync(group);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(Messages.Logging.GroupCreated, group.Id);

            // Build response DTO
            var groupDto = new GroupDto
            {
                Id = group.Id,
                CourseId = group.CourseId,
                CourseName = course.Name,
                Name = group.Name,
                Description = group.Description,
                MaxMembers = group.MaxMembers,
                IsLocked = group.IsLocked,
                AssignmentId = group.AssignmentId,
                MemberCount = 0,
                CreatedAt = group.CreatedAt,
                CreatedBy = group.CreatedBy
            };

            return new CreateGroupResponse
            {
                Success = true,
                Message = Messages.Success.GroupCreated,
                GroupId = group.Id,
                Group = groupDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating group for course {CourseId}", request.CourseId);
            return new CreateGroupResponse
            {
                Success = false,
                Message = Messages.Helpers.FormatError(Messages.Error.GroupCreationFailed, ex.Message)
            };
        }
    }
}
