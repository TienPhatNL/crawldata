using MediatR;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Application.Features.Assignments.Queries;

public class GetAssignmentGroupsQueryHandler : IRequestHandler<GetAssignmentGroupsQuery, GetAssignmentGroupsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;

    public GetAssignmentGroupsQueryHandler(IUnitOfWork unitOfWork, IKafkaUserService userService)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
    }

    public async Task<GetAssignmentGroupsResponse> Handle(GetAssignmentGroupsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var assignment = await _unitOfWork.Assignments
                .GetAssignmentWithGroupsAsync(request.AssignmentId, cancellationToken);

            if (assignment == null)
            {
                return new GetAssignmentGroupsResponse
                {
                    Success = false,
                    Message = "Assignment not found",
                    AssignmentId = request.AssignmentId,
                    AssignmentTitle = string.Empty,
                    Groups = new List<GroupDto>(),
                    TotalGroups = 0
                };
            }

            var groupDtos = new List<GroupDto>();
            
            foreach (var g in assignment.AssignedGroups ?? new List<Domain.Entities.Group>())
            {
                // Find the leader
                var leader = g.Members?.FirstOrDefault(m => m.Role == GroupMemberRole.Leader);
                string? leaderName = null;
                Guid? leaderId = null;

                if (leader != null)
                {
                    leaderId = leader.StudentId;
                    var leaderInfo = await _userService.GetUserByIdAsync(leader.StudentId);
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

            groupDtos = groupDtos.OrderBy(g => g.Name).ToList();

            return new GetAssignmentGroupsResponse
            {
                Success = true,
                Message = $"Found {groupDtos.Count} group(s) assigned to this assignment",
                AssignmentId = request.AssignmentId,
                AssignmentTitle = assignment.Title,
                Groups = groupDtos,
                TotalGroups = groupDtos.Count
            };
        }
        catch (Exception ex)
        {
            return new GetAssignmentGroupsResponse
            {
                Success = false,
                Message = $"Error retrieving assignment groups: {ex.Message}",
                AssignmentId = request.AssignmentId,
                AssignmentTitle = string.Empty,
                Groups = new List<GroupDto>(),
                TotalGroups = 0
            };
        }
    }
}
