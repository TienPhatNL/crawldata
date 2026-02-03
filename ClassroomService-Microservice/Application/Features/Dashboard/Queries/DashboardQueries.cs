using ClassroomService.Domain.DTOs;
using MediatR;

namespace ClassroomService.Application.Features.Dashboard.Queries;

public class GetStudentCourseGradesQuery : IRequest<DashboardResponse<CourseGradesDetailDto>>
{
    public Guid CourseId { get; set; }
}

public class GetPendingAssignmentsQuery : IRequest<DashboardResponse<PendingAssignmentsDto>>
{
    public Guid? TermId { get; set; }
}

public class GetCoursesQuery : IRequest<DashboardResponse<CurrentCoursesDto>>
{
    public Guid TermId { get; set; }
}

public class GetStudentPerformanceAnalyticsQuery : IRequest<DashboardResponse<StudentPerformanceAnalyticsDto>>
{
    public Guid? TermId { get; set; }
}

public class GetLecturerCoursesOverviewQuery : IRequest<DashboardResponse<LecturerCoursesOverviewDto>>
{
    public Guid? TermId { get; set; }
}

public class GetGradingQueueQuery : IRequest<DashboardResponse<GradingQueueDto>>
{
    public Guid? CourseId { get; set; }
}

public class GetCourseStudentPerformanceQuery : IRequest<DashboardResponse<CourseStudentPerformanceDto>>
{
    public Guid CourseId { get; set; }
}

public class GetAssignmentStatisticsQuery : IRequest<DashboardResponse<AssignmentStatisticsDto>>
{
    public Guid CourseId { get; set; }
}
