using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Events;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.Groups.Commands;

public class RandomizeStudentsToGroupsCommandHandler : IRequestHandler<RandomizeStudentsToGroupsCommand, RandomizeStudentsToGroupsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IKafkaUserService _userService;
    private readonly ILogger<RandomizeStudentsToGroupsCommandHandler> _logger;

    public RandomizeStudentsToGroupsCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IKafkaUserService userService,
        ILogger<RandomizeStudentsToGroupsCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _userService = userService;
        _logger = logger;
    }

    public async Task<RandomizeStudentsToGroupsResponse> Handle(RandomizeStudentsToGroupsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue)
            {
                return new RandomizeStudentsToGroupsResponse
                {
                    Success = false,
                    Message = Messages.Error.UserIdNotFound
                };
            }

            // Validate group size
            if (request.GroupSize < 1)
            {
                return new RandomizeStudentsToGroupsResponse
                {
                    Success = false,
                    Message = Messages.Error.InvalidGroupSize
                };
            }

            // Get course and verify lecturer
            var course = await _unitOfWork.Courses
                .GetAsync(c => c.Id == request.CourseId, cancellationToken);

            if (course == null)
            {
                return new RandomizeStudentsToGroupsResponse
                {
                    Success = false,
                    Message = Messages.Error.CourseNotFound
                };
            }

            // Verify user is the lecturer
            if (course.LecturerId != currentUserId.Value)
            {
                return new RandomizeStudentsToGroupsResponse
                {
                    Success = false,
                    Message = Messages.Error.OnlyLecturerCanManageGroups
                };
            }

            _logger.LogInformation(Messages.Logging.RandomizingStudents, "calculating", request.GroupSize, request.CourseId);

            // Get all enrolled students in this course
            var enrollments = await _unitOfWork.CourseEnrollments.GetEnrollmentsByCourseAsync(request.CourseId, cancellationToken);
            var activeEnrollments = enrollments
                .Where(e => e.Status == EnrollmentStatus.Active)
                .ToList();

            if (!activeEnrollments.Any())
            {
                return new RandomizeStudentsToGroupsResponse
                {
                    Success = false,
                    Message = Messages.Error.NoEnrolledStudents
                };
            }

            var enrollmentDict = activeEnrollments.ToDictionary(e => e.StudentId, e => e);
            var enrolledStudentIds = activeEnrollments.Select(e => e.StudentId).ToList();

            // Get students already in groups for this course
            var groupMembers = await _unitOfWork.GroupMembers.GetMembersByCourseAsync(request.CourseId, cancellationToken);
            var studentsInGroups = groupMembers
                .Select(gm => gm.Enrollment.StudentId)
                .Distinct()
                .ToList();

            var studentsInGroupsSet = new HashSet<Guid>(studentsInGroups);

            // Filter to get only students NOT in any group
            var availableStudents = enrolledStudentIds
                .Where(sid => !studentsInGroupsSet.Contains(sid))
                .ToList();

            if (!availableStudents.Any())
            {
                return new RandomizeStudentsToGroupsResponse
                {
                    Success = false,
                    Message = Messages.Error.NoAvailableStudentsForRandomization
                };
            }

            _logger.LogInformation("Found {Count} available students for randomization", availableStudents.Count);

            // Shuffle students randomly
            var random = new Random();
            var shuffledStudents = availableStudents.OrderBy(_ => random.Next()).ToList();

            // Calculate number of groups
            var studentCount = shuffledStudents.Count;
            var groupCount = (int)Math.Ceiling((double)studentCount / request.GroupSize);

            // Calculate distribution
            var baseSize = studentCount / groupCount;
            var remainder = studentCount % groupCount;

            _logger.LogInformation("Creating {GroupCount} groups for {StudentCount} students (base size: {BaseSize}, extras: {Remainder})",
                groupCount, studentCount, baseSize, remainder);

            // Get student information for leader assignment
            var students = await _userService.GetUsersByIdsAsync(shuffledStudents, cancellationToken);
            var studentDict = students.ToDictionary(s => s.Id);

            var groups = new List<RandomizedGroupInfo>();
            var currentIndex = 0;

            // Create groups and assign students
            for (int i = 0; i < groupCount; i++)
            {
                // Determine group size (first 'remainder' groups get an extra member)
                var thisGroupSize = baseSize + (i < remainder ? 1 : 0);

                // Create group with MaxMembers set to the calculated group size
                var group = new Group
                {
                    CourseId = request.CourseId,
                    Name = $"Group {i + 1}",
                    Description = $"Randomly generated group {i + 1}",
                    MaxMembers = thisGroupSize, // Set max members to prevent group from growing beyond initial size
                    IsLocked = false,
                    CreatedBy = currentUserId.Value
                };

                await _unitOfWork.Groups.AddAsync(group, cancellationToken);

                // Get students for this group
                var groupStudents = shuffledStudents.Skip(currentIndex).Take(thisGroupSize).ToList();

                // Randomly select leader from group members
                var leaderIndex = random.Next(groupStudents.Count);
                var leaderId = groupStudents[leaderIndex];

                // Add members to group
                for (int j = 0; j < groupStudents.Count; j++)
                {
                    var studentId = groupStudents[j];
                    var isLeader = j == leaderIndex;
                    var enrollment = enrollmentDict[studentId];

                    var member = new GroupMember
                    {
                        GroupId = group.Id,
                        EnrollmentId = enrollment.Id,
                        IsLeader = isLeader,
                        Role = isLeader ? GroupMemberRole.Leader : GroupMemberRole.Member,
                        JoinedAt = DateTime.UtcNow,
                        CreatedBy = currentUserId.Value
                    };

                    // Add domain event with all group members
                    member.AddDomainEvent(new GroupMemberAddedEvent(
                        group.Id,
                        enrollment.Id,
                        studentId,
                        request.CourseId,
                        isLeader,
                        currentUserId.Value,
                        groupStudents, // All student IDs in this group
                        group.Name
                    ));

                    await _unitOfWork.GroupMembers.AddAsync(member, cancellationToken);
                }

                // Get leader info for response
                var leaderUser = studentDict.ContainsKey(leaderId) ? studentDict[leaderId] : null;
                var leaderName = leaderUser != null ? $"{leaderUser.LastName} {leaderUser.FirstName}".Trim() : "Unknown";

                groups.Add(new RandomizedGroupInfo
                {
                    Id = group.Id,
                    Name = group.Name,
                    MemberCount = thisGroupSize,
                    LeaderId = leaderId,
                    LeaderName = leaderName
                });

                currentIndex += thisGroupSize;
            }

            // Save all changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(Messages.Logging.GroupsRandomized, groupCount, studentCount);

            return new RandomizeStudentsToGroupsResponse
            {
                Success = true,
                Message = string.Format(Messages.Success.GroupsRandomizedSuccess, studentCount, groupCount),
                CourseId = request.CourseId,
                GroupsCreated = groupCount,
                StudentsAssigned = studentCount,
                Groups = groups
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error randomizing students into groups for course {CourseId}", request.CourseId);
            return new RandomizeStudentsToGroupsResponse
            {
                Success = false,
                Message = Messages.Helpers.FormatError(Messages.Error.RandomizeGroupsFailed, ex.Message)
            };
        }
    }
}

