using ClassroomService.Domain.DTOs;
using MediatR;

namespace ClassroomService.Application.Features.Dashboard.Queries;

public class GetStudentGradesOverviewQuery : IRequest<DashboardResponse<StudentGradesOverviewDto>>
{
    public Guid? TermId { get; set; }
}
