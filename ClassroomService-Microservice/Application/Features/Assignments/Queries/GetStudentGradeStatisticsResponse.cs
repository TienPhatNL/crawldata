using ClassroomService.Application.Features.Assignments.DTOs;

namespace ClassroomService.Application.Features.Assignments.Queries;

public class GetStudentGradeStatisticsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public StudentGradeStatisticsDto? Statistics { get; set; }
}
