using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.Reports.Queries;

public class ExportAssignmentGradesQueryHandler : IRequestHandler<ExportAssignmentGradesQuery, ExportAssignmentGradesResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IExcelService _excelService;

    public ExportAssignmentGradesQueryHandler(IUnitOfWork unitOfWork, IExcelService excelService)
    {
        _unitOfWork = unitOfWork;
        _excelService = excelService;
    }

    public async Task<ExportAssignmentGradesResponse> Handle(ExportAssignmentGradesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var assignment = await _unitOfWork.Assignments.GetAsync(a => a.Id == request.AssignmentId, cancellationToken);
            if (assignment == null)
            {
                return new ExportAssignmentGradesResponse { Success = false, Message = "Assignment not found" };
            }

            var reports = await _unitOfWork.Reports.GetReportsForExportAsync(request.AssignmentId, cancellationToken);
            
            var exportData = reports.Select(r => new
            {
                StudentOrGroup = r.IsGroupSubmission ? $"Group: {r.Group?.Name}" : $"Student: {r.SubmittedBy}",
                SubmittedAt = r.SubmittedAt?.ToString("yyyy-MM-dd HH:mm"),
                Status = r.Status.ToString(),
                Grade = r.Grade?.ToString() ?? "Not Graded",
                MaxPoints = assignment.MaxPoints?.ToString() ?? "N/A",
                Feedback = r.Feedback ?? "",
                GradedAt = r.GradedAt?.ToString("yyyy-MM-dd HH:mm") ?? "",
                Version = r.Version,
                IsLate = r.Status == Domain.Enums.ReportStatus.Late ? "Yes" : "No"
            }).ToList();

            var fileContent = _excelService.ExportToExcel(exportData, $"{assignment.Title} - Grades");
            var fileName = $"{assignment.Title.Replace(" ", "_")}_Grades_{DateTime.Now:yyyyMMdd}.xlsx";

            return new ExportAssignmentGradesResponse
            {
                Success = true,
                Message = "Grades exported successfully",
                FileContent = fileContent,
                FileName = fileName
            };
        }
        catch (Exception ex)
        {
            return new ExportAssignmentGradesResponse { Success = false, Message = $"Error: {ex.Message}" };
        }
    }
}
