using MediatR;
using Microsoft.Extensions.Logging;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Application.Features.Groups.Queries;

public class GetGroupByIdQueryHandler : IRequestHandler<GetGroupByIdQuery, GetGroupByIdResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;
    private readonly ILogger<GetGroupByIdQueryHandler> _logger;

    public GetGroupByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IKafkaUserService userService,
        ILogger<GetGroupByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
        _logger = logger;
    }

    public async Task<GetGroupByIdResponse> Handle(GetGroupByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Get group with all related data including enrollments
            var group = await _unitOfWork.Groups.GetGroupWithMembersAsync(request.GroupId, cancellationToken);

            if (group == null)
            {
                return new GetGroupByIdResponse
                {
                    Success = false,
                    Message = Messages.Error.GroupNotFound
                };
            }

            // Get leader information if exists
            var leader = group.Members?.FirstOrDefault(m => m.IsLeader);
            string? leaderName = null;
            Guid? leaderId = null;

            if (leader != null && leader.Enrollment != null)
            {
                leaderId = leader.Enrollment.StudentId;
                var leaderUser = await _userService.GetUserByIdAsync(leader.Enrollment.StudentId, cancellationToken);
                if (leaderUser != null)
                {
                    leaderName = $"{leaderUser.LastName} {leaderUser.FirstName}".Trim();
                }
            }

            // Build group DTO with null-safe access
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
                AssignmentTitle = group.Assignment?.Title,
                MemberCount = group.Members?.Count ?? 0,
                LeaderName = leaderName,
                LeaderId = leaderId,
                CreatedAt = group.CreatedAt,
                CreatedBy = group.CreatedBy
            };

            return new GetGroupByIdResponse
            {
                Success = true,
                Message = Messages.Success.GroupRetrieved,
                Group = groupDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving group {GroupId}", request.GroupId);
            return new GetGroupByIdResponse
            {
                Success = false,
                Message = Messages.Helpers.FormatError(Messages.Error.GroupRetrievalFailed, ex.Message)
            };
        }
    }
}
