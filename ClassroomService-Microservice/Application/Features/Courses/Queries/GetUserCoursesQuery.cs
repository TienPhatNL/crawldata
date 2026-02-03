using MediatR;

namespace ClassroomService.Application.Features.Courses.Queries;

public class GetUserCoursesQuery : IRequest<GetUserCoursesResponse>
{
    public Guid UserId { get; set; }
    public bool AsLecturer { get; set; } = false; // false = as student, true = as lecturer
}