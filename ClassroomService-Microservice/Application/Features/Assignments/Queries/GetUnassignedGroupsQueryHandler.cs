using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Enums;
using MediatR;

namespace ClassroomService.Application.Features.Assignments.Queries;

public class GetUnassignedGroupsQueryHandler : IRequestHandler<GetUnassignedGroupsQuery, GetUnassignedGroupsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;

    public GetUnassignedGroupsQueryHandler(IUnitOfWork unitOfWork, IKafkaUserService userService)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
    }

    public async Task<GetUnassignedGroupsResponse> Handle(GetUnassignedGroupsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var course = await _unitOfWork.Courses
                .GetAsync(c => c.Id == request.CourseId, cancellationToken);

            if (course == null)
            {
                return new GetUnassignedGroupsResponse
                {
                    Success = false,
                    Message = "Course not found",
                    CourseId = request.CourseId,
                    CourseName = string.Empty,
                    UnassignedGroups = new List<GroupDto>(),
                    TotalGroups = 0,
                    AssignedGroupsCount = 0,
                    UnassignedGroupsCount = 0
                };
            }

            // Get all groups in the course with members
            var allGroups = await _unitOfWork.Groups
                .GetManyAsync(
                    g => g.CourseId == request.CourseId, 
                    cancellationToken,
                    g => g.Members,
                    g => g.Course);

            var allGroupsList = allGroups.ToList();

            // Filter unassigned groups (those without an AssignmentId)
            var unassignedGroupsList = allGroupsList
                .Where(g => !g.AssignmentId.HasValue)
                .ToList();

            var unassignedGroupDtos = new List<GroupDto>();

            foreach (var g in unassignedGroupsList)
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

                unassignedGroupDtos.Add(new GroupDto
                {
                    Id = g.Id,
                    CourseId = g.CourseId,
                    CourseName = g.Course?.Name ?? course?.Name ?? "Unknown Course",
                    Name = g.Name,
                    Description = g.Description ?? string.Empty,
                    MaxMembers = g.MaxMembers,
                    IsLocked = g.IsLocked,
                    AssignmentId = g.AssignmentId,
                    MemberCount = g.Members?.Count ?? 0,
                    LeaderId = leaderId,
                    LeaderName = leaderName,
                    CreatedAt = g.CreatedAt
                });
            }

            unassignedGroupDtos = unassignedGroupDtos.OrderBy(g => g.Name).ToList();

            var totalGroups = allGroupsList.Count;
            var assignedGroupsCount = allGroupsList.Count(g => g.AssignmentId.HasValue);
            var unassignedGroupsCount = unassignedGroupDtos.Count;

            return new GetUnassignedGroupsResponse
            {
                Success = true,
                Message = $"Found {unassignedGroupsCount} unassigned group(s) out of {totalGroups} total groups",
                CourseId = request.CourseId,
                CourseName = course?.Name ?? "Unknown Course",
                UnassignedGroups = unassignedGroupDtos,
                TotalGroups = totalGroups,
                AssignedGroupsCount = assignedGroupsCount,
                UnassignedGroupsCount = unassignedGroupsCount
            };
        }
        catch (Exception ex)
        {
            return new GetUnassignedGroupsResponse
            {
                Success = false,
                Message = $"Error retrieving unassigned groups: {ex.Message}",
                CourseId = request.CourseId,
                CourseName = string.Empty,
                UnassignedGroups = new List<GroupDto>(),
                TotalGroups = 0,
                AssignedGroupsCount = 0,
                UnassignedGroupsCount = 0
            };
        }
    }
}

