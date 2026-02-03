using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClassroomService.Application.Features.Groups.Queries;

public class GetMyGroupsQueryHandler : IRequestHandler<GetMyGroupsQuery, GetMyGroupsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetMyGroupsQueryHandler> _logger;

    public GetMyGroupsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetMyGroupsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GetMyGroupsResponse> Handle(GetMyGroupsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Get group memberships for the student by querying through Enrollment
            var groupMemberships = await _unitOfWork.GroupMembers
                .GetManyAsync(
                    gm => gm.Enrollment.StudentId == request.StudentId &&
                          (!request.CourseId.HasValue || gm.Group.CourseId == request.CourseId.Value),
                    cancellationToken: cancellationToken,
                    gm => gm.Enrollment,
                    gm => gm.Group,
                    gm => gm.Group.Course,
                    gm => gm.Group.Course.CourseCode,
                    gm => gm.Group.Assignment
                );

            var groupMembershipsList = groupMemberships.ToList();

            if (!groupMembershipsList.Any())
            {
                var emptyMessage = request.CourseId.HasValue
                    ? $"You are not a member of any groups in this course"
                    : "You are not a member of any groups";

                return new GetMyGroupsResponse
                {
                    Success = true,
                    Message = emptyMessage,
                    Groups = new List<StudentGroupMembershipDto>(),
                    TotalGroups = 0
                };
            }

            // Load member counts for each group
            foreach (var gm in groupMembershipsList)
            {
                if (gm.Group != null && gm.Group.Members == null)
                {
                    var members = await _unitOfWork.GroupMembers.GetMembersByGroupAsync(gm.GroupId, cancellationToken);
                    gm.Group.Members = members.ToList();
                }
            }

            // Map to DTOs
            var groupDtos = groupMembershipsList.Select(gm =>
            {
                var group = gm.Group;
                return new StudentGroupMembershipDto
                {
                    GroupId = group.Id,
                    GroupName = group.Name,
                    Description = group.Description,
                    CourseId = group.CourseId,
                    CourseName = group.Course?.Name,
                    CourseCode = group.Course?.CourseCode?.Code,
                    IsLocked = group.IsLocked,
                    MaxMembers = group.MaxMembers,
                    MemberCount = group.Members?.Count ?? 0,
                    AssignmentId = group.AssignmentId,
                    AssignmentTitle = group.Assignment?.Title,
                    IsLeader = gm.IsLeader,
                    Role = gm.Role.ToString(),
                    JoinedAt = gm.JoinedAt,
                    CreatedAt = group.CreatedAt
                };
            }).ToList();

            var successMessage = request.CourseId.HasValue
                ? $"Successfully retrieved {groupDtos.Count} groups for the specified course"
                : $"Successfully retrieved {groupDtos.Count} groups";

            _logger.LogInformation("Retrieved {Count} groups for student {StudentId}" +
                (request.CourseId.HasValue ? " in course {CourseId}" : ""),
                groupDtos.Count, request.StudentId, request.CourseId);

            return new GetMyGroupsResponse
            {
                Success = true,
                Message = successMessage,
                Groups = groupDtos,
                TotalGroups = groupDtos.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving groups for student {StudentId}", request.StudentId);
            return new GetMyGroupsResponse
            {
                Success = false,
                Message = $"Error retrieving groups: {ex.Message}",
                Groups = new List<StudentGroupMembershipDto>(),
                TotalGroups = 0
            };
        }
    }
}
