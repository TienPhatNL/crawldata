using ClassroomService.Domain.Constants;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.GroupMembers.Queries;

public class GetGroupMembersQueryHandler : IRequestHandler<GetGroupMembersQuery, GetGroupMembersResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;
    private readonly ILogger<GetGroupMembersQueryHandler> _logger;

    public GetGroupMembersQueryHandler(
        IUnitOfWork unitOfWork,
        IKafkaUserService userService,
        ILogger<GetGroupMembersQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
        _logger = logger;
    }

    public async Task<GetGroupMembersResponse> Handle(GetGroupMembersQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Verify group exists and load members with enrollments
            var group = await _unitOfWork.Groups.GetGroupWithMembersAsync(request.GroupId, cancellationToken);

            if (group == null)
            {
                return new GetGroupMembersResponse
                {
                    Success = false,
                    Message = Messages.Error.GroupNotFound,
                    Members = new List<GroupMemberDto>()
                };
            }

            if (!group.Members.Any())
            {
                return new GetGroupMembersResponse
                {
                    Success = true,
                    Message = Messages.Info.NoMembersFound,
                    Members = new List<GroupMemberDto>()
                };
            }

            // Get student IDs from enrollments
            var studentIds = group.Members
                .Select(m => m.Enrollment.StudentId)
                .Distinct()
                .ToList();

            // Fetch student information in batch
            var students = await _userService.GetUsersByIdsAsync(studentIds, cancellationToken);
            var studentDict = students.ToDictionary(s => s.Id);

            // Build DTOs
            var memberDtos = group.Members.Select(m =>
            {
                var studentId = m.Enrollment.StudentId;
                var student = studentDict.ContainsKey(studentId) ? studentDict[studentId] : null;

                return new GroupMemberDto
                {
                    Id = m.Id,
                    GroupId = m.GroupId,
                    GroupName = group.Name,
                    EnrollmentId = m.EnrollmentId,
                    StudentId = studentId,
                    StudentName = student != null ? $"{student.LastName} {student.FirstName}".Trim() : "Unknown",
                    StudentEmail = student?.Email ?? "unknown@email.com",
                    IsLeader = m.IsLeader,
                    Role = m.Role,
                    RoleDisplay = m.Role.ToString(),
                    JoinedAt = m.JoinedAt,
                    Notes = m.Notes
                };
            })
            .OrderByDescending(m => m.IsLeader)
            .ThenBy(m => m.StudentName)
            .ToList();

            return new GetGroupMembersResponse
            {
                Success = true,
                Message = Messages.Success.MembersRetrieved,
                Members = memberDtos
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving members for group {GroupId}", request.GroupId);
            return new GetGroupMembersResponse
            {
                Success = false,
                Message = Messages.Helpers.FormatError(Messages.Error.MembersRetrievalFailed, ex.Message),
                Members = new List<GroupMemberDto>()
            };
        }
    }
}
