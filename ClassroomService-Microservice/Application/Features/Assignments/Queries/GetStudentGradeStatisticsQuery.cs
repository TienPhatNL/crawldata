using MediatR;
using ClassroomService.Application.Features.Assignments.DTOs;

namespace ClassroomService.Application.Features.Assignments.Queries;

/// <summary>
/// Query to get grade statistics for the authenticated student in a specific course
/// </summary>
public record GetStudentGradeStatisticsQuery(
    Guid CourseId,
    Guid RequestUserId,
    string RequestUserRole
) : IRequest<GetStudentGradeStatisticsResponse>;
