using ClassroomService.Domain.DTOs;
using MediatR;

namespace ClassroomService.Application.Features.Chat.Queries;

public class GetCourseUsersQuery : IRequest<GetCourseUsersResponse>
{
    public Guid CourseId { get; set; }
    public Guid UserId { get; set; }
    public string UserRole { get; set; } = string.Empty;
}

public class GetCourseUsersResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<UserDto> Users { get; set; } = new();
}
