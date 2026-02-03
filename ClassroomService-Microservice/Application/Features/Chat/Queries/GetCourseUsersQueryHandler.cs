using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.Chat.Queries;

public class GetCourseUsersQueryHandler : IRequestHandler<GetCourseUsersQuery, GetCourseUsersResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;

    public GetCourseUsersQueryHandler(IUnitOfWork unitOfWork, IKafkaUserService userService)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
    }

    public async Task<GetCourseUsersResponse> Handle(GetCourseUsersQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate access to course
            var enrollment = await _unitOfWork.CourseEnrollments
                .GetEnrollmentAsync(request.CourseId, request.UserId);
            var course = await _unitOfWork.Courses.GetByIdAsync(request.CourseId);

            var isStaff = request.UserRole == "Staff" || request.UserRole == "Admin";
            
            if (enrollment == null && course?.LecturerId != request.UserId && !isStaff)
            {
                return new GetCourseUsersResponse
                {
                    Success = false,
                    Message = "Access denied to this course",
                    Users = new List<UserDto>()
                };
            }

            // Get all enrollments and lecturer
            var enrollments = await _unitOfWork.CourseEnrollments.GetEnrollmentsByCourseAsync(request.CourseId);
            var userIds = enrollments.Select(e => e.StudentId).ToList();

            if (course != null)
            {
                userIds.Add(course.LecturerId);
            }

            // Remove current user
            userIds = userIds.Where(id => id != request.UserId).Distinct().ToList();

            // Get conversations for this course to find latest message timestamps
            var conversations = await _unitOfWork.Conversations
                .GetUserConversationsAsync(request.UserId, request.CourseId);

            // Get all unread counts in a single query for performance
            var allUnreadCounts = await _unitOfWork.Chats
                .GetUnreadCountsAsync(request.UserId, cancellationToken);

            // Create dictionaries for quick lookup
            var userMessageTimestamps = new Dictionary<Guid, DateTime>();
            var conversationIdByUserId = new Dictionary<Guid, Guid>();
            
            foreach (var conv in conversations)
            {
                var otherUserId = conv.User1Id == request.UserId ? conv.User2Id : conv.User1Id;
                userMessageTimestamps[otherUserId] = conv.LastMessageAt;
                conversationIdByUserId[otherUserId] = conv.Id;
            }

            // Get user details and attach message timestamps + unread counts
            var usersWithTimestamps = new List<(UserDto user, DateTime? lastMessage, int unreadCount)>();
            foreach (var id in userIds)
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user != null)
                {
                    var lastMessageTime = userMessageTimestamps.ContainsKey(id)
                        ? userMessageTimestamps[id]
                        : (DateTime?)null;
                    
                    // Get unread count for this user's conversation
                    var unreadCount = 0;
                    if (conversationIdByUserId.TryGetValue(id, out var convId))
                    {
                        allUnreadCounts.TryGetValue(convId, out unreadCount);
                    }
                    
                    usersWithTimestamps.Add((user, lastMessageTime, unreadCount));
                }
            }

            // Order by latest message (users with messages first, then alphabetically by name)
            var orderedUsers = usersWithTimestamps
                .OrderByDescending(u => u.lastMessage.HasValue)  // Users with messages first
                .ThenByDescending(u => u.lastMessage ?? DateTime.MinValue)  // Then by latest message
                .ThenBy(u => u.user.FullName)  // Then alphabetically
                .Select(u =>
                {
                    u.user.UnreadCount = u.unreadCount;
                    return u.user;
                })
                .ToList();

            return new GetCourseUsersResponse
            {
                Success = true,
                Message = "Users retrieved successfully",
                Users = orderedUsers
            };
        }
        catch (Exception ex)
        {
            return new GetCourseUsersResponse
            {
                Success = false,
                Message = $"Error retrieving users: {ex.Message}",
                Users = new List<UserDto>()
            };
        }
    }
}
