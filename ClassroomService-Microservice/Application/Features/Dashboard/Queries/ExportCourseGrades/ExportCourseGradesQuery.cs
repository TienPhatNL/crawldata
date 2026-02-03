using MediatR;

namespace ClassroomService.Application.Features.Dashboard.Queries.ExportCourseGrades;

public record ExportCourseGradesQuery(Guid CourseId) : IRequest<byte[]>;
