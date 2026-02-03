using ClassroomService.Domain.Constants;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Infrastructure.Persistence;
using MediatR;

namespace ClassroomService.Application.Features.GroupMembers.Queries;

public class GetGroupMemberByIdQueryHandler : IRequestHandler<GetGroupMemberByIdQuery, GetGroupMemberByIdResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;
    private readonly ILogger<GetGroupMemberByIdQueryHandler> _logger;

    public GetGroupMemberByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IKafkaUserService userService,
        ILogger<GetGroupMemberByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
        _logger = logger;
    }

    public async Task<GetGroupMemberByIdResponse> Handle(GetGroupMemberByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Get group member with group and enrollment information loaded
            var member = await _unitOfWork.GroupMembers
                .GetAsync(m => m.Id == request.MemberId, cancellationToken, m => m.Group, m => m.Enrollment);

            if (member == null)
            {
                return new GetGroupMemberByIdResponse
                {
                    Success = false,
                    Message = Messages.Error.MemberNotFound
                };
            }

            // Null check for Group navigation property
            if (member.Group == null)
            {
                _logger.LogError("GroupMember {MemberId} has null Group navigation property", request.MemberId);
                return new GetGroupMemberByIdResponse
                {
                    Success = false,
                    Message = "Group member data is incomplete"
                };
            }

            // Null check for Enrollment navigation property
            if (member.Enrollment == null)
            {
                _logger.LogError("GroupMember {MemberId} has null Enrollment navigation property", request.MemberId);
                return new GetGroupMemberByIdResponse
                {
                    Success = false,
                    Message = "Group member data is incomplete"
                };
            }

            var studentId = member.Enrollment.StudentId;

            // Get student information
            var student = await _userService.GetUserByIdAsync(studentId, cancellationToken);

            // Build member DTO
            var memberDto = new GroupMemberDto
            {
                Id = member.Id,
                GroupId = member.GroupId,
                GroupName = member.Group.Name,
                EnrollmentId = member.EnrollmentId,
                StudentId = studentId,
                StudentName = student != null ? $"{student.LastName} {student.FirstName}".Trim() : "Unknown",
                StudentEmail = student?.Email ?? "unknown@email.com",
                IsLeader = member.IsLeader,
                Role = member.Role,
                RoleDisplay = member.Role.ToString(),
                JoinedAt = member.JoinedAt,
                Notes = member.Notes
            };

            return new GetGroupMemberByIdResponse
            {
                Success = true,
                Message = "Group member retrieved successfully",
                Member = memberDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving group member {MemberId}", request.MemberId);
            return new GetGroupMemberByIdResponse
            {
                Success = false,
                Message = Messages.Helpers.FormatError(Messages.Error.MembersRetrievalFailed, ex.Message)
            };
        }
    }
}
