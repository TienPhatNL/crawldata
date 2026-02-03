using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Application.Features.Dashboard.Helpers;
using ClassroomService.Application.Services;
using MediatR;

namespace ClassroomService.Application.Features.Dashboard.Queries.ExportCourseGrades;

public class ExportCourseGradesQueryHandler : IRequestHandler<ExportCourseGradesQuery, byte[]>
{
    private readonly IGradeExportService _exportService;
    private readonly TermAccessValidator _termValidator;
    private readonly ICurrentUserService _currentUserService;

    public ExportCourseGradesQueryHandler(
        IGradeExportService exportService,
        TermAccessValidator termValidator,
        ICurrentUserService currentUserService)
    {
        _exportService = exportService;
        _termValidator = termValidator;
        _currentUserService = currentUserService;
    }

    public async Task<byte[]> Handle(ExportCourseGradesQuery request, CancellationToken cancellationToken)
    {
        var lecturerId = _currentUserService.UserId!.Value;

        // Validate lecturer owns this course
        var isAuthorized = await _termValidator.ValidateLecturerOwnsCourseAsync(
            lecturerId, 
            request.CourseId, 
            cancellationToken);

        if (!isAuthorized)
        {
            throw new UnauthorizedAccessException("You do not have permission to export grades for this course");
        }

        return await _exportService.ExportCourseGradesToExcelAsync(request.CourseId, cancellationToken);
    }
}
