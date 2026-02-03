using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Events;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.GroupMembers.Commands;

public class AddMultipleGroupMembersCommandHandler : IRequestHandler<AddMultipleGroupMembersCommand, AddMultipleGroupMembersResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IKafkaUserService _userService;
    private readonly ILogger<AddMultipleGroupMembersCommandHandler> _logger;

    public AddMultipleGroupMembersCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IKafkaUserService userService,
        ILogger<AddMultipleGroupMembersCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _userService = userService;
        _logger = logger;
    }

    public async Task<AddMultipleGroupMembersResponse> Handle(AddMultipleGroupMembersCommand request, CancellationToken cancellationToken)
    {
        var results = new List<BulkAddResult>();
        var successCount = 0;
        var failureCount = 0;

        try
        {
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue)
            {
                return new AddMultipleGroupMembersResponse
                {
                    Success = false,
                    Message = Messages.Error.UserIdNotFound,
                    TotalRequested = request.StudentIds.Count,
                    FailureCount = request.StudentIds.Count
                };
            }

            // Use GetGroupWithMembersAsync to properly load all related data
            var group = await _unitOfWork.Groups.GetGroupWithMembersAsync(request.GroupId, cancellationToken);

            if (group == null)
            {
                return new AddMultipleGroupMembersResponse
                {
                    Success = false,
                    Message = Messages.Error.GroupNotFound,
                    TotalRequested = request.StudentIds.Count,
                    FailureCount = request.StudentIds.Count
                };
            }

            // Null check for Course
            if (group.Course == null)
            {
                _logger.LogError("Group {GroupId} has null Course navigation property", request.GroupId);
                return new AddMultipleGroupMembersResponse
                {
                    Success = false,
                    Message = "Group data is incomplete",
                    TotalRequested = request.StudentIds.Count,
                    FailureCount = request.StudentIds.Count
                };
            }

            // Verify user is the lecturer
            if (group.Course.LecturerId != currentUserId.Value)
            {
                return new AddMultipleGroupMembersResponse
                {
                    Success = false,
                    Message = Messages.Error.OnlyLecturerCanManageGroups,
                    TotalRequested = request.StudentIds.Count,
                    FailureCount = request.StudentIds.Count
                };
            }

            // Check if group is locked
            if (group.IsLocked)
            {
                return new AddMultipleGroupMembersResponse
                {
                    Success = false,
                    Message = Messages.Error.GroupLocked,
                    TotalRequested = request.StudentIds.Count,
                    FailureCount = request.StudentIds.Count
                };
            }

            _logger.LogInformation(Messages.Logging.AddingMultipleMembers, request.StudentIds.Count, request.GroupId);

            // Get all students from UserService
            var students = await _userService.GetUsersByIdsAsync(request.StudentIds, cancellationToken);
            var studentDict = students.Where(s => s.Role == RoleConstants.Student)
                                     .ToDictionary(s => s.Id);

            // Get all enrollments for this course
            var enrollments = await _unitOfWork.CourseEnrollments.GetEnrollmentsByCourseAsync(group.CourseId, cancellationToken);
            var activeEnrollments = enrollments
                .Where(e => e.Status == EnrollmentStatus.Active)
                .ToList();

            var enrollmentDict = activeEnrollments.ToDictionary(e => e.StudentId, e => e);

            // Get students already in ANY group in this course
            var allGroupsInCourse = await _unitOfWork.Groups.GetGroupsByCourseAsync(group.CourseId, cancellationToken);
            var studentsInGroups = allGroupsInCourse
                .SelectMany(g => g.Members ?? new List<GroupMember>())
                .Select(gm => gm.Enrollment.StudentId)
                .Distinct()
                .ToList();

            var studentsInGroupsSet = new HashSet<Guid>(studentsInGroups);

            // Get current member IDs in THIS group
            var currentMemberIds = new HashSet<Guid>((group.Members ?? new List<GroupMember>()).Select(m => m.Enrollment.StudentId));

            // Calculate available slots
            int? availableSlots = null;
            if (group.MaxMembers.HasValue)
            {
                availableSlots = group.MaxMembers.Value - (group.Members?.Count ?? 0);
                if (availableSlots <= 0)
                {
                    return new AddMultipleGroupMembersResponse
                    {
                        Success = false,
                        Message = Messages.Error.GroupFull,
                        TotalRequested = request.StudentIds.Count,
                        FailureCount = request.StudentIds.Count
                    };
                }
            }

            // Process each student
            foreach (var studentId in request.StudentIds)
            {
                // Check if we've reached capacity
                if (availableSlots.HasValue && successCount >= availableSlots.Value)
                {
                    results.Add(new BulkAddResult
                    {
                        StudentId = studentId,
                        Success = false,
                        Message = Messages.Error.GroupFull
                    });
                    failureCount++;
                    continue;
                }

                // Validate student exists and has correct role
                if (!studentDict.ContainsKey(studentId))
                {
                    results.Add(new BulkAddResult
                    {
                        StudentId = studentId,
                        Success = false,
                        Message = Messages.Error.StudentNotFound
                    });
                    failureCount++;
                    continue;
                }

                // Check if student has active enrollment in the course
                if (!enrollmentDict.ContainsKey(studentId))
                {
                    results.Add(new BulkAddResult
                    {
                        StudentId = studentId,
                        Success = false,
                        Message = string.Format(Messages.Error.StudentNotEnrolledInCourse, studentId)
                    });
                    failureCount++;
                    continue;
                }

                var enrollment = enrollmentDict[studentId];

                // Check if already in THIS group
                if (currentMemberIds.Contains(studentId))
                {
                    results.Add(new BulkAddResult
                    {
                        StudentId = studentId,
                        Success = false,
                        Message = Messages.Error.MemberAlreadyInGroup
                    });
                    failureCount++;
                    continue;
                }

                // Check if already in ANOTHER group in this course
                if (studentsInGroupsSet.Contains(studentId))
                {
                    results.Add(new BulkAddResult
                    {
                        StudentId = studentId,
                        Success = false,
                        Message = string.Format(Messages.Error.StudentAlreadyInAnotherGroup, studentId)
                    });
                    failureCount++;
                    continue;
                }

                // Add the member - all as regular members (IsLeader = false, Role = Member)
                var student = studentDict[studentId];
                var isLeader = false; // Will be updated if this student is the leader
                var member = new GroupMember
                {
                    GroupId = request.GroupId,
                    EnrollmentId = enrollment.Id,
                    IsLeader = isLeader,
                    Role = isLeader ? GroupMemberRole.Leader : GroupMemberRole.Member,
                    JoinedAt = DateTime.UtcNow
                };

                // Track this new member for event
                currentMemberIds.Add(studentId);

                // Add domain event with all current member IDs
                member.AddDomainEvent(new GroupMemberAddedEvent(
                    member.GroupId,
                    member.EnrollmentId,
                    enrollment.StudentId,
                    group.CourseId,
                    false,
                    currentUserId.Value,
                    currentMemberIds.ToList(), // All members including new one
                    group.Name
                ));

                await _unitOfWork.GroupMembers.AddAsync(member);
                studentsInGroupsSet.Add(studentId);

                // Build member DTO for response
                var memberDto = new GroupMemberDto
                {
                    Id = member.Id,
                    GroupId = member.GroupId,
                    GroupName = group.Name,
                    EnrollmentId = member.EnrollmentId,
                    StudentId = enrollment.StudentId,
                    StudentName = $"{student.LastName} {student.FirstName}".Trim(),
                    StudentEmail = student.Email,
                    IsLeader = false,
                    Role = GroupMemberRole.Member,
                    RoleDisplay = "Member",
                    JoinedAt = member.JoinedAt
                };

                results.Add(new BulkAddResult
                {
                    StudentId = studentId,
                    Success = true,
                    Message = Messages.Success.MemberAdded,
                    Member = memberDto
                });

                successCount++;
            }

            // Save all changes at once
            if (successCount > 0)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Successfully added {SuccessCount} of {TotalCount} students to group {GroupId}",
                    successCount, request.StudentIds.Count, request.GroupId);
            }

            var overallSuccess = successCount > 0;
            var message = successCount == request.StudentIds.Count
                ? string.Format(Messages.Success.MembersAdded, successCount)
                : string.Format(Messages.Success.MembersAddedBulk, successCount, request.StudentIds.Count);

            return new AddMultipleGroupMembersResponse
            {
                Success = overallSuccess,
                Message = message,
                TotalRequested = request.StudentIds.Count,
                SuccessCount = successCount,
                FailureCount = failureCount,
                Results = results
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding multiple members to group {GroupId}", request.GroupId);
            return new AddMultipleGroupMembersResponse
            {
                Success = false,
                Message = Messages.Helpers.FormatError(Messages.Error.MemberAddFailed, ex.Message),
                TotalRequested = request.StudentIds.Count,
                SuccessCount = successCount,
                FailureCount = request.StudentIds.Count - successCount,
                Results = results
            };
        }
    }
}

