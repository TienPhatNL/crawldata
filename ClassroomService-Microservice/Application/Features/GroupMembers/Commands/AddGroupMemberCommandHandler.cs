using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Events;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.GroupMembers.Commands;

public class AddGroupMemberCommandHandler : IRequestHandler<AddGroupMemberCommand, AddGroupMemberResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IKafkaUserService _userService;
    private readonly ILogger<AddGroupMemberCommandHandler> _logger;

    public AddGroupMemberCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IKafkaUserService userService,
        ILogger<AddGroupMemberCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _userService = userService;
        _logger = logger;
    }

    public async Task<AddGroupMemberResponse> Handle(AddGroupMemberCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue)
            {
                return new AddGroupMemberResponse
                {
                    Success = false,
                    Message = Messages.Error.UserIdNotFound
                };
            }

            // Get group with members
            var group = await _unitOfWork.Groups.GetGroupWithMembersAsync(request.GroupId, cancellationToken);

            if (group == null)
            {
                return new AddGroupMemberResponse
                {
                    Success = false,
                    Message = Messages.Error.GroupNotFound
                };
            }

            // Get course information
            var course = await _unitOfWork.Courses.GetAsync(c => c.Id == group.CourseId, cancellationToken);
            if (course == null)
            {
                return new AddGroupMemberResponse
                {
                    Success = false,
                    Message = "Course not found"
                };
            }

            // Verify user is the lecturer
            if (course.LecturerId != currentUserId.Value)
            {
                return new AddGroupMemberResponse
                {
                    Success = false,
                    Message = Messages.Error.OnlyLecturerCanManageGroups
                };
            }

            // Check if group is locked
            if (group.IsLocked)
            {
                return new AddGroupMemberResponse
                {
                    Success = false,
                    Message = Messages.Error.GroupLocked
                };
            }

            // Check if group is full
            if (group.MaxMembers.HasValue && group.Members.Count >= group.MaxMembers.Value)
            {
                return new AddGroupMemberResponse
                {
                    Success = false,
                    Message = Messages.Error.GroupFull
                };
            }

            // Validate student exists and has correct role
            var student = await _userService.GetUserByIdAsync(request.StudentId, cancellationToken);
            if (student == null || student.Role != RoleConstants.Student)
            {
                return new AddGroupMemberResponse
                {
                    Success = false,
                    Message = Messages.Error.StudentNotFound
                };
            }

            // Get the active enrollment for this student in the course
            var enrollment = await _unitOfWork.CourseEnrollments
                .GetAsync(e => e.CourseId == group.CourseId 
                    && e.StudentId == request.StudentId 
                    && e.Status == EnrollmentStatus.Active, cancellationToken);

            if (enrollment == null)
            {
                return new AddGroupMemberResponse
                {
                    Success = false,
                    Message = Messages.Error.NotEnrolledInCourse
                };
            }

            // Check if student is already in this group (by enrollmentId)
            var alreadyInGroup = await _unitOfWork.GroupMembers
                .ExistsAsync(m => m.GroupId == request.GroupId && m.EnrollmentId == enrollment.Id, cancellationToken);

            if (alreadyInGroup)
            {
                return new AddGroupMemberResponse
                {
                    Success = false,
                    Message = Messages.Error.MemberAlreadyInGroup
                };
            }

            // Check if student is already in another group in the same course
            var existingGroupMember = await _unitOfWork.GroupMembers
                .GetAsync(m => m.EnrollmentId == enrollment.Id 
                    && m.Group.CourseId == group.CourseId, 
                    cancellationToken,
                    m => m.Group);

            if (existingGroupMember != null)
            {
                return new AddGroupMemberResponse
                {
                    Success = false,
                    Message = $"Student is already a member of group '{existingGroupMember.Group.Name}' in this course. A student can only be in one group per course."
                };
            }

            _logger.LogInformation(Messages.Logging.MemberAdding, request.StudentId, request.GroupId);

            // Create group member with EnrollmentId
            // Ensure Role is consistent with IsLeader flag
            var role = request.IsLeader ? GroupMemberRole.Leader : request.Role;
            
            var member = new GroupMember
            {
                Id = Guid.NewGuid(),
                GroupId = request.GroupId,
                EnrollmentId = enrollment.Id,
                IsLeader = request.IsLeader,
                Role = role,
                JoinedAt = DateTime.UtcNow,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUserId.Value
            };

            // Get all group member IDs including the new one
            var existingMemberIds = (await _unitOfWork.GroupMembers
                .GetManyAsync(gm => gm.GroupId == request.GroupId, cancellationToken))
                .Select(gm => gm.Enrollment.StudentId)
                .ToList();
            existingMemberIds.Add(enrollment.StudentId); // Add the new member

            // Add domain event
            member.AddDomainEvent(new GroupMemberAddedEvent(
                member.GroupId,
                member.EnrollmentId,
                enrollment.StudentId,
                group.CourseId,
                member.IsLeader,
                currentUserId.Value,
                existingMemberIds,
                group.Name
            ));

            await _unitOfWork.GroupMembers.AddAsync(member, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(Messages.Logging.MemberAdded, request.StudentId, request.GroupId);

            // Build response DTO
            var memberDto = new GroupMemberDto
            {
                Id = member.Id,
                GroupId = member.GroupId,
                GroupName = group.Name,
                EnrollmentId = member.EnrollmentId,
                StudentId = enrollment.StudentId,
                StudentName = $"{student.LastName} {student.FirstName}".Trim(),
                StudentEmail = student.Email,
                IsLeader = member.IsLeader,
                Role = member.Role,
                RoleDisplay = member.Role.ToString(),
                JoinedAt = member.JoinedAt,
                Notes = member.Notes
            };

            return new AddGroupMemberResponse
            {
                Success = true,
                Message = Messages.Success.MemberAdded,
                MemberId = member.Id,
                Member = memberDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding member {StudentId} to group {GroupId}", request.StudentId, request.GroupId);
            return new AddGroupMemberResponse
            {
                Success = false,
                Message = Messages.Helpers.FormatError(Messages.Error.MemberAddFailed, ex.Message)
            };
        }
    }
}
