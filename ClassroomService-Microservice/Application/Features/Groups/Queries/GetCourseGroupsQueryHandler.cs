using ClassroomService.Domain.Constants;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClassroomService.Application.Features.Groups.Queries;

public class GetCourseGroupsQueryHandler : IRequestHandler<GetCourseGroupsQuery, GetCourseGroupsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;
    private readonly ILogger<GetCourseGroupsQueryHandler> _logger;

    public GetCourseGroupsQueryHandler(
        IUnitOfWork unitOfWork,
        IKafkaUserService userService,
        ILogger<GetCourseGroupsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
        _logger = logger;
    }

    public async Task<GetCourseGroupsResponse> Handle(GetCourseGroupsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Verify course exists
            var course = await _unitOfWork.Courses.GetByIdAsync(request.CourseId, cancellationToken);

            if (course == null)
            {
                return new GetCourseGroupsResponse
                {
                    Success = false,
                    Message = Messages.Error.CourseNotFound,
                    Groups = new List<GroupDto>()
                };
            }

            // Get all groups with members
            var groups = (await _unitOfWork.Groups.GetGroupsByCourseAsync(request.CourseId, cancellationToken))
                .OrderBy(g => g.Name)
                .ToList();

            if (!groups.Any())
            {
                return new GetCourseGroupsResponse
                {
                    Success = true,
                    Message = Messages.Info.NoGroupsFound,
                    Groups = new List<GroupDto>()
                };
            }

            // Get all unique leader IDs
            var leaderIds = groups
                .SelectMany(g => g.Members.Where(m => m.IsLeader).Select(m => m.StudentId))
                .Distinct()
                .ToList();

            // Fetch leader information in batch
            Dictionary<Guid, string> leaderNames = new();
            if (leaderIds.Any())
            {
                var leaders = await _userService.GetUsersByIdsAsync(leaderIds, cancellationToken);
                leaderNames = leaders.ToDictionary(
                    l => l.Id,
                    l => $"{l.LastName} {l.FirstName}".Trim()
                );
            }

            // Build DTOs
            var groupDtos = groups.Select(g =>
            {
                var leader = g.Members.FirstOrDefault(m => m.IsLeader);
                var leaderName = leader != null && leaderNames.ContainsKey(leader.StudentId)
                    ? leaderNames[leader.StudentId]
                    : null;

                return new GroupDto
                {
                    Id = g.Id,
                    CourseId = g.CourseId,
                    CourseName = course.Name,
                    Name = g.Name,
                    Description = g.Description,
                    MaxMembers = g.MaxMembers,
                    IsLocked = g.IsLocked,
                    AssignmentId = g.AssignmentId,
                    AssignmentTitle = g.Assignment?.Title,
                    MemberCount = g.Members.Count,
                    LeaderName = leaderName,
                    LeaderId = leader?.StudentId,
                    CreatedAt = g.CreatedAt,
                    CreatedBy = g.CreatedBy
                };
            }).ToList();

            return new GetCourseGroupsResponse
            {
                Success = true,
                Message = Messages.Success.GroupsRetrieved,
                Groups = groupDtos
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving groups for course {CourseId}", request.CourseId);
            return new GetCourseGroupsResponse
            {
                Success = false,
                Message = Messages.Helpers.FormatError(Messages.Error.GroupsRetrievalFailed, ex.Message),
                Groups = new List<GroupDto>()
            };
        }
    }
}

