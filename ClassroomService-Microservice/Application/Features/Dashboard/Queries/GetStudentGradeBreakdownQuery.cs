using ClassroomService.Domain.DTOs;
using MediatR;

namespace ClassroomService.Application.Features.Dashboard.Queries;

public class GetStudentGradeBreakdownQuery : IRequest<DashboardResponse<StudentGradeBreakdownDto>>
{
    public Guid CourseId { get; set; }
}
